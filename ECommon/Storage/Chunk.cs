using ECommon.Components;
using ECommon.Logging;
using ECommon.Storage.Exceptions;
using ECommon.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace ECommon.Storage
{
    public unsafe class Chunk:IDisposable
    {
        #region Private Variables

        private static readonly ILogger _logger= ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(Chunk));
        private ChunkHeader _chunkHeader;
        private ChunkFooter _chunkFooter;

        private readonly string _filename;
        private readonly ChunkManager _chunkManager;
        private readonly ChunkManagerConfig _chunkConfig;
        private readonly bool _isMemoryChunk;
        private readonly ConcurrentQueue<ReaderWorkItem> _readerWorkItemQueue = new ConcurrentQueue<ReaderWorkItem>();

        private readonly object _writeSyncObj = new object();
        private readonly object _cacheSyncObj = new object();
        private readonly object _freeMemoryObj = new object();

        private int _dataPosition;
        private int _bloomFilterSize;
        private bool _isCompleted;
        private bool _isDestroying;
        private bool _isMemoryFreed;
        private int _cachingChunk;
        private DateTime _lastActiveTime;
        private bool _isReadersInitialized;
        private int _flushedDataPosition;

        private Chunk _memoryChunk;
        private CacheItem[] _cacheItems;
        private IntPtr _cachedData;
        private int _cachedLength;

        private WriterWorkItem _writerWorkItem;

        #endregion

        #region Public Properties

        public string FileName { get { return _filename; } }
        public ChunkHeader ChunkHeader { get { return _chunkHeader; } }
        public ChunkFooter ChunkFooter { get { return _chunkFooter; } }
        public ChunkManagerConfig Config { get { return _chunkConfig; } }
        public bool IsCompleted { get { return _isCompleted; } }
        public DateTime LastActiveTime
        {
            get
            {
                var lastActiveTimeOfMemoryChunk = DateTime.MinValue;
                if (_memoryChunk != null)
                {
                    lastActiveTimeOfMemoryChunk = _memoryChunk.LastActiveTime;
                }
                return lastActiveTimeOfMemoryChunk>=_lastActiveTime? lastActiveTimeOfMemoryChunk : _lastActiveTime;
            }
        }
        public bool IsMemoryChunk { get { return _isMemoryChunk; } }
        public bool HasCachedChunk { get { return _memoryChunk!= null; } }
        public int DataPosition { get { return _dataPosition; } }
        public long GlobalDataPosition
        {
            get
            {
                return ChunkHeader.ChunkDataStartPosition + DataPosition;
            }
        }
        public bool IsFixedDataSize()
        {
            return _chunkConfig.ChunkDataUnitSize > 0 && _chunkConfig.ChunkDataCount > 0;
        }
        #endregion

        #region Constructors

        public Chunk(string filename,ChunkManager chunkManager,ChunkManagerConfig chunkConfig,bool isMemoryChunk)
        {
            Ensure.NotNullOrEmpty(filename, "filename");
            Ensure.NotNull(chunkManager, "chunkManager");
            Ensure.NotNull(chunkConfig, "chunkConfig");

            _filename = filename;
            _chunkManager = chunkManager;
            _chunkConfig = chunkConfig;
            _isMemoryChunk = isMemoryChunk;
            _lastActiveTime = DateTime.Now;
        }

        ~Chunk()
        {
            UnCacheFromMemory();
        }

        #endregion

        #region Factory Methods

        public static Chunk CreateNew(string filename,int chunkNumber,ChunkManager chunkManager,ChunkManagerConfig config,bool isMemoryChunk)
        {
            var chunk = new Chunk(filename, chunkManager, config, isMemoryChunk);

            try { chunk.Ini}
        }

        #endregion

        #region InitMethods

        private void InitCompleted()
        {
            var fileInfo = new FileInfo(_filename);
            if (!fileInfo.Exists)
            {
                throw new ChunkFileNotExistException(_filename);
            }

            _isCompleted = true;

            using(var fileStream=new FileStream(_filename,FileMode.Open,FileAccess.Read,FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None))
            {
                using(var reader=new BinaryReader(fileStream))
                {
                    _chunkHeader = ReadHeader(fileStream, reader);
                    _chunkFooter = ReadFooter(fileStream, reader);

                    CheckCompleteFileChunk();
                }
            }
        }

        #endregion

        #region Helper Methods

        private void CheckCompleteFileChunk()
        {
            using(var fileStream=new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None))
            {
                //检查chunk文件的实际大小是否正确
                var chunkFileSize = ChunkHeader.Size + _chunkFooter.ChunkDataTotalSize + ChunkFooter.Size;
                if (chunkFileSize != fileStream.Length)
                {
                    throw new ChunkBadDataException(
                        string.Format("The size of chunk {0} should be equals with fileStream's length {1}, but instead it was {2}.",
                                        this,
                                        fileStream.Length,
                                        chunkFileSize));
                }

                //如果chunk中的数据是固定大小的，则还需要检查数据总数是否正确
                if (IsFixedDataSize())
                {
                    if (_chunkFooter.ChunkDataTotalSize != _chunkHeader.ChunkDataTotalSize)
                    {
                        throw new ChunkBadDataException(
                           string.Format("For fixed-size chunk, the total data size of chunk {0} should be {1}, but instead it was {2}.",
                                           this,
                                           _chunkHeader.ChunkDataTotalSize,
                                           _chunkFooter.ChunkDataTotalSize));
                    }
                }
            }
        }

        private void LoadFileChunkToMemory()
        {
            using (var fileStream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 8192, FileOptions.None))
            {
                var cachedLength = (int)fileStream.Length;
                //Marshal:提供用于分配非托管内存、复制非托管内存块和将托管类型转换为非托管类型的方法集合，以及与非托管代码交互时使用的其他杂项方法
                //AllocHGlobal: 使用指定的字节数从进程的非托管内存中分配内存
                var cachedData = Marshal.AllocHGlobal(cachedLength);

                try
                {
                    //UnmanagedMemoryStream:提供从托管代码访问非托管内存块的能力
                    using (var unmanagedStream = new UnmanagedMemoryStream((byte*)_cachedData, cachedLength, cachedLength, FileAccess.ReadWrite))
                    {
                        fileStream.Seek(0, SeekOrigin.Begin);
                        var buffer = new byte[65536];
                        int toRead = _cachedLength;
                        while (toRead > 0)
                        {
                            int read = fileStream.Read(buffer, 0, Math.Min(toRead, buffer.Length));
                            if (read == 0)
                            {
                                break;
                            }
                            toRead -= read;
                            unmanagedStream.Write(buffer, 0, read);
                        }
                    }
                }
                catch
                {
                    Marshal.FreeHGlobal(cachedData);
                    throw;
                }

                _cachedData = cachedData;
                _cachedLength = cachedLength;
            }
        }

        private void FreeMemory()
        {
            if (_isMemoryChunk && !_isMemoryFreed) {
                lock (_freeMemoryObj)
                {
                    var cachedData=Interlocked.Exchange(ref _cachedData, IntPtr.Zero);
                    if (cachedData != IntPtr.Zero)
                    {
                        try {
                            Marshal.FreeHGlobal(cachedData);
                        }
                        catch(Exception ex)
                        {
                            _logger.Error(string.Format("Failed to free memory of chunk {0}", this), ex);
                        }
                    }
                    _isMemoryFreed = true;
                }
            }
        }

        private void InitializeReaderWorkItems()
        {
            for(var i = 0; i < _chunkConfig.ChunkReaderCount; i++)
            {
                _readerWorkItemQueue.Enqueue(CreateReaderWorkItem());
            }
            _isReadersInitialized = true;
        }

        private void CloseAllReaderWorkItems()
        {
            if (!_isReadersInitialized) return;

            //Stopwatch:提供一组方法和属性，您可以使用这些方法和属性精确地测量经过的时间
            var watch = Stopwatch.StartNew();
            var closedCount = 0;
            while (closedCount < _chunkConfig.ChunkReaderCount)
            {
                ReaderWorkItem readerWorkItem;
                while(_readerWorkItemQueue.TryDequeue(out readerWorkItem))
                {
                    readerWorkItem.Reader.Close();
                    closedCount++;
                }

                if (closedCount >= _chunkConfig.ChunkReaderCount)
                {
                    break;
                }
                Thread.Sleep(1000);

                if (watch.ElapsedMilliseconds > 30 * 1000)
                {
                    _logger.ErrorFormat("Close chunk reader work items timeout, expect close count: {0}, real close count: {1}", _chunkConfig.ChunkReaderCount, closedCount);
                    break;
                }
            }
        }

        private ReaderWorkItem CreateReaderWorkItem()
        {
            var stream = default(Stream);
            if (_isMemoryChunk)
            {
                stream=new UnmanagedMemoryStream((byte*)_cachedData, _cachedLength);
            }
            else
            {
                stream = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, _chunkConfig.ChunkReadBuffer, FileOptions.None);
            }
            return new ReaderWorkItem(stream, new BinaryReader(stream));
        }

        private ReaderWorkItem GetReaderWorkItem()
        {
            ReaderWorkItem readerWorkItem;
            while(!_readerWorkItemQueue.TryDequeue(out readerWorkItem))
            {
                Thread.Sleep(1);
            }
            return readerWorkItem;
        }

        private void ReturnReaderWorkItem(ReaderWorkItem readerWorkItem)
        {
            _readerWorkItemQueue.Enqueue(readerWorkItem);
        }

        private ChunkFooter WriteFooter()
        {
            var currentTotalDataSize = DataPosition;
            var bloomFilterSize = _bloomFilterSize;

            //如果食固定大小得数据，则检查总数据大小是否正确
            if (IsFixedDataSize())
            {
                if (currentTotalDataSize != _chunkHeader.ChunkDataTotalSize)
                {
                    throw new ChunkCompleteException(string.Format("Cannot write the chunk footer as the current total data size is incorrect. chunk: {0}, expectTotalDataSize: {1}, currentTotalDataSize: {2}",
                        this,
                        _chunkHeader.ChunkDataTotalSize,
                        currentTotalDataSize));
                }
            }

            var workItem = _writerWorkItem;
            var footer = new ChunkFooter(currentTotalDataSize);

            workItem.AppendData(footer.AsByteArray(), 0, ChunkFooter.Size);

            Flush();

            var oldStreamLength = workItem.WorkingStream.Length;
            var newStreamLength = ChunkHeader.Size + currentTotalDataSize + bloomFilterSize + ChunkFooter.Size;

            if (newStreamLength != oldStreamLength)
            {
                workItem.ResizeStream(newStreamLength);
            }

            return footer;
        }

        #endregion


        class CacheItem
        {
            public long RecordPosition;
            public byte[] RecordBuffer;
        }

        class ChunkFileStream : IStream
        {
            public Stream Stream;
            public FlushOption FlushOption;

            public ChunkFileStream(Stream stream,FlushOption flushOption)
            {
                Stream = stream;
                FlushOption = FlushOption;
            }

            public long Length
            {
                get
                {
                    return Stream.Length;
                }
            }

            public long Position
            {
                get
                {
                    return Stream.Position;
                }
                set
                {
                    Stream.Position = value;
                }
            }

            public void Dispose()
            {
                Stream.Dispose();
            }

            public void Flush()
            {
                var fileStream = Stream as FileStream;
                if (fileStream != null)
                {
                    if (FlushOption == FlushOption.FlushToDisk)
                    {
                        //清除此流的缓冲区并将所有缓冲数据写入文件，同时清除所有中间文件缓冲区
                        fileStream.Flush(true);
                    }
                    else
                    {
                        //清除此流的缓冲区并将所有缓冲数据写入文件
                        fileStream.Flush();
                    }
                }
                else
                {
                    //在派生类中重写时，清除此流的所有缓冲区，并将所有缓冲数据写入基础设备。
                    Stream.Flush();
                }
            }

            public void SetLength(long value)
            {
                Stream.SetLength(value);
            }

            public void Write(byte[] buffer,int offset,int count)
            {
                Stream.Write(buffer, offset, count);
            }
        }

        public override string ToString()
        {
            return string.Format("({0}-#{1})", _chunkManager.Name, _chunkHeader.ChunkNumber);
        }

    }
}
