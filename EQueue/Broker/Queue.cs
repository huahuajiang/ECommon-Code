using ECommon.Components;
using ECommon.Logging;
using ECommon.Serializing;
using ECommon.Storage;
using EQueue.Protocols.Brokers;
using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Broker
{
    public interface IQueue
    {
        string Topic { get; }
        int QueueId { get; }
        long NextOffest { get; }
        long IncrementNextOffset();
    }

    public class Queue:IQueue
    {
        private const string QueueSettingFileName = "queue.setting";
        private readonly ChunkWriter _chunkWriter;
        private readonly ChunkReader _chunkReader;
        private readonly ChunkManager _chunkManager;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly string _queueSettingFile;
        private QueueSetting _setting;
        private long _nextoffest = 0;
        private ILogger _logger;

        public string Topic { get; private set; }
        public int QueueId { get; private set; }
        public long NextOffset { get { return _nextoffest; } }
        public QueueSetting Setting { get { return _setting; } }
        public QueueKey Key { get; private set; }

        public Queue(string topic,int queueId)
        {
            Topic = topic;
            QueueId = queueId;
            Key = new QueueKey(topic, queueId);

            _jsonSerializer = ObjectContainer.Resolve<IJsonSerializer>();
            _chunkManager = new ChunkManager(
                "QueueChunk-" + Key.ToString(),
                BrokerController.Instance.Setting.
        }
    }

    public class QueueSetting
    {
        public bool ProducerVisible;
        public bool ConsumerVisible;
        public bool IsDeleted;

        public QueueSetting()
        {
            ProducerVisible = true;
            ConsumerVisible = true;
            IsDeleted = false;
        }
    }
}
