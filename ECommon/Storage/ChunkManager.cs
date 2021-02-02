using ECommon.Components;
using ECommon.Logging;
using ECommon.Scheduling;
using ECommon.Utilities;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ECommon.Storage
{
    public class ChunkManager:IDisposable
    {
        private static readonly ILogger _logger= ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(ChunkManager));
        private readonly object _lockObj = new object();
        private readonly ChunkManagerConfig _config;
        private readonly IDictionary<int, Chunk> _chunks;
        private readonly string _chunkPath;
        private readonly IScheduleService _scheduleService;
        private readonly bool _isMemoryMode;
        private int _nextChunkNumber;
        private int _uncachingChunks;
        private int _isCachingNextChunk;
        private ConcurrentDictionary<int, BytesInfo> _bytesWriteDict;
        private ConcurrentDictionary<int, CountInfo> _fileReadDict;
        private ConcurrentDictionary<int, CountInfo> _unmanagedReadDict;
        private ConcurrentDictionary<int, CountInfo> _cachedReadDict;

        class BytesInfo
        {
            public long PreviousBytes;
            public long CurrentBytes;

            public long UpgradeBytes()
            {
                var incrementBytes = CurrentBytes - PreviousBytes;
                PreviousBytes = CurrentBytes;
                return incrementBytes;
            }
        }

        class CountInfo
        {
            public long PreviousCount;
            public long CurrentCount;

            public long UpgradeCount()
            {
                var incrementCount = CurrentCount - PreviousCount;
                PreviousCount = CurrentCount;
                return incrementCount;
            }
        }

        public string Name { get; private set; }
        public ChunkManagerConfig Config { get { return _config; } }
        public string ChunkPath { get { return _chunkPath; } }
        public bool IsMemory { get { return _isMemoryMode; } }
        public ChunkManager(string name,ChunkManagerConfig config,bool isMemoryMode,IEnumerable<string> relativePaths = null)
        {
            Ensure.NotNull(name, "name");
            Ensure.NotNull(config, "config");

            Name = name;
            _config = config;
            _isMemoryMode = isMemoryMode;
            if (relativePaths == null)
            {
                _chunkPath = _config.BasePath;
            }
            else
            {
                var chunkPath = _config.BasePath;
                foreach(var relativePath in relativePaths)
                {
                    chunkPath = Path.Combine(chunkPath, relativePath);
                }
                _chunkPath = chunkPath;
            }
            if (!Directory.Exists(_chunkPath))
            {
                Directory.CreateDirectory(_chunkPath);
            }
            _chunks = new ConcurrentDictionary<int, Chunk>();
            _scheduleService = ObjectContainer.Resolve<IScheduleService>();
            _byteWriteDict = new ConcurrentDictionary<int, BytesInfo>();
            _fileReadDict = new ConcurrentDictionary<int, CountInfo>();
            _unmanagedReadDict = new ConcurrentDictionary<int, CountInfo>();
            _cachedReadDict = new ConcurrentDictionary<int, CountInfo>();
        }

        public void Load<T>(Func<byte[],T> readRecordFunc) where T : ILogRecord
        {
            if (_isMemoryMode) return;
            lock (_lockObj)
            {
                if (!Directory.Exists(_chunkPath))
                {
                    Directory.CreateDirectory(_chunkPath);
                }

                var tempFiles = _config.FileNamingStrategy.GetTempFiles(_chunkPath);
                foreach(var file in tempFiles)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }

                var files = _config.FileNamingStrategy.GetChunkFiles(_chunkPath);
                if (files.Length > 0)
                {
                    var cachedChunkCount = 0;
                    for(var i = files.Length - 2; i >= 0; i--)
                    {
                        var file = files[i];
                        var chunk= Chunk.FromCompletedFile(file, this, _config, _isMemoryMode);
                        if (_config.EnableCache && cachedChunkCount < _config.PreCacheChunkCount)
                        {
                            if (chunk.TryCacheInMemory(false))
                            {
                                cachedChunkCount++;
                            }
                        }
                        AddChunk(chunk);
                    }
                    var lastFile = files[files.Length - 1];
                    AddChunk(Chunk.FromOngoingFile(lastFile, this, _config, readRecordFunc, _isMemoryMode));
                }

                if (_config.EnableCache)
                {
                    _scheduleService.StartTask("UncacheChunks", () => UncacheChunks(), 1000, 1000);
                }
            }
        }


        public int GetChunkCount()
        {
            return _chunks.Count;
        }

        public IList<Chunk> GetAllChunks()
        {
            return _chunks.Values.ToList();
        }

        public Chunk AddNewChunk()
        {
            lock (_lockObj)
            {
                var chunkNumber = _nextChunkNumber;
                var chunkFileName = _config.FileNamingStrategy.GetFileNameFor(_chunkPath, chunkNumber);
                var chunk = Chunk.CreateNew(chunkFileName, chunkNumber, this, _config, _isMemoryMode);

                AddChunk(chunk);

                return chunk;
            }
        }

        public Chunk GetFirstChunk()
        {
            lock (_lockObj)
            {
                if (_chunks.Count == 0)
                {
                    AddNewChunk();
                }
                var minChunkNum = _chunks.Keys.Min();
                return _chunks[minChunkNum];
            }
        }

        public Chunk GetLastChunk()
        {
            lock (_lockObj)
            {
                if (_chunks.Count == 0)
                {
                    AddNewChunk();
                }
                return _chunks[_nextChunkNumber - 1];
            }
        }

        public int GetChunkNum(long dataPosition)
        {
            return (int)(dataPosition / _config.GetChunkDataSize());
        }

        public Chunk GetChunkFor(long dataPosition)
        {
            var chunkNum = (int)(dataPosition / _config.GetChunkDataSize());
            return GetChunk(chunkNum);
        }

        public Chunk GetChunk(int chunkNum)
        {
            if (_chunks.ContainsKey(chunkNum))
            {
                return _chunks[chunkNum];
            }
            return null;
        }

        public bool RemoveChunk(Chunk chunk)
        {
            lock (_lockObj)
            {
                if (_chunks.Remove(chunk.ChunkHeader.ChunkNumber))
                {
                    try
                    {
                        chunk.Destroy();
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(string.Format("Destroy chunk {0} has exception.", chunk), ex);
                    }
                    return true;
                }
                return false;
            }
        }

        public void TryCacheNextChunk(Chunk currentChunk)
        {
            if (!_config.EnableCache) return;

            if(Interlocked.CompareExchange(ref _isCachingNextChunk, 1, 0) == 0)
            {
                try
                {
                    var nextChunkNumber = currentChunk.ChunkHeader.ChunkNumber + 1;
                    var nextChunk = GetChunk(nextChunkNumber);
                    if (nextChunk != null && !nextChunk.IsMemoryChunk && nextChunk.IsCompleted && !nextChunk.HasCachedChunk)
                    {
                        Task.Factory.StartNew(() => nextChunk.TryCacheInMemory(false));
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _isCachingNextChunk, 0);
                }
            }
        }

        public void Start()
        {
            if (_config.EnableChunkStatistic)
            {
                _scheduleService.StartTask("LogChunkStatisticStatus", LogChunkStatisticStatus, 1000, 1000);
            }
        }

        public void Shutdown()
        {
            if (_config.EnableChunkStatistic)
            {
                _scheduleService.StopTask("LogChunkStatisticStatus");
            }
        }

        public void AddWriteBytes(int chunkNum,int byteCount)
        {
            _bytesWriteDict.AddOrUpdate(chunkNum, GetDefaultBytesInfo, (chunkNumber, current) => UpfateBytesInfo(chunkNumber, current, byteCount));
        }

        public void AddFileReadCount(int chunkNum)
        {
            _fileReadDict.AddOrUpdate(chunkNum, GetDefaultCountInfo, UpdateCountInfo);
        }

        public void AddUnmanageReadCount(int chunkNum)
        {

        }
    }
}