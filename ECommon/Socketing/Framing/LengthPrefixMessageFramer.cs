using ECommon.Components;
using ECommon.Logging;
using System;
using System.Collections.Generic;

namespace ECommon.Socketing.Framing
{
    public class LengthPrefixMessageFramer : IMessageFramer
    {
        private static readonly ILogger _logger= ObjectContainer.Resolve<ILoggerFactory>().Create(typeof(LengthPrefixMessageFramer).FullName);

        public const int HeaderLength = sizeof(Int32);//32位，4个字节（byte) ,
        private Action<ArraySegment<byte>> _receivedHandler;

        private byte[] _messageBuffer;
        private int _bufferIndex = 0;
        private int _headerBytes = 0;
        private int _packageLength = 0;

        public IEnumerable<ArraySegment<byte>> FrameData(ArraySegment<byte> data)
        {
            var length = data.Count;
            //yield关键字用于遍历循环中，yield return用于返回IEnumerable<T>,yield break用于终止循环遍历。是"按需供给"
            yield return new ArraySegment<byte>(new[] { (byte)length, (byte)(length >> 8), (byte)(length >> 16), (byte)(length >> 24) });
            yield return data;
        }

        public void RegisterMessageArrivedCallback(Action<ArraySegment<byte>> handler)
        {
            _receivedHandler = handler ?? throw new ArgumentNullException("handler");
        }

        public void UnFrameData(IEnumerable<ArraySegment<byte>> data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            foreach(ArraySegment<byte> buffer in data)
            {
                Parse(buffer);
            }
        }

        public void UnFrameData(ArraySegment<byte> data)
        {
            Parse(data);
        }

        private void Parse(ArraySegment<byte> bytes)
        {
            byte[] data = bytes.Array;
            for (int i = bytes.Offset, n = bytes.Offset + bytes.Count; i < n; i++) {
                if (_headerBytes < HeaderLength) {
                    //按位或
                    _packageLength |= (data[i] << (_headerBytes * 8));
                    ++_headerBytes;
                    if (_headerBytes == HeaderLength)
                    {
                        if (_packageLength <= 0)
                        {
                            throw new Exception(string.Format("Package length ({0}) is out of bounds.", _packageLength));
                        }
                        _messageBuffer = new byte[_packageLength];
                    }
                }
                else
                {
                    int copyCnt = Math.Min(bytes.Count + bytes.Offset - i, _packageLength - _bufferIndex);
                    try
                    {
                        Buffer.BlockCopy(bytes.Array, i, _messageBuffer, _bufferIndex, copyCnt);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(string.Format("Parse message buffer failed, _headerLength: {0}, _packageLength: {1}, _bufferIndex: {2}, copyCnt: {3}, _messageBuffer is null: {4}",
                        _headerBytes,
                        _packageLength,
                        _bufferIndex,
                        copyCnt,
                        _messageBuffer == null), ex);
                        throw;
                    }
                    _bufferIndex += copyCnt;
                    i += copyCnt - 1;
                    if (_bufferIndex == _packageLength)
                    {
                        if (_receivedHandler != null)
                        {
                            try
                            {
                                _receivedHandler(new ArraySegment<byte>(_messageBuffer, 0, _bufferIndex));
                            }
                            catch (Exception ex)
                            {
                                _logger.Error("Handle received message fail.", ex);
                            }
                        }
                        _messageBuffer = null;
                        _headerBytes = 0;
                        _packageLength = 0;
                        _bufferIndex = 0;
                    }
                }
            }
        }
    }
}
