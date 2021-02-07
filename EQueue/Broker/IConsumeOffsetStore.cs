using EQueue.Protocols.Brokers;
using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Broker
{
    public interface IConsumeOffsetStore
    {
        void Start();
        void Shutdown();
        int GetConsumerGroupCount();
        IEnumerable<string> GetAllConsumerGroupNames();
        bool DeleteConsumerGroup(string group);
        long GetConsumeOffset(string topic, int queueId, string group);
        long GetMinConsumedOffset(string topic, int queueId);
        void UpdateConsumeOffset(string topic, int queueId, long offset, string group);
        void DeleteConsumeOffset(QueueKey queueKey);
        void SetConsumeNextOffset(string topic, int queuueId, string group, long nextOffset);
        bool TryFetchNextConsumeOffset(string topic, int queueId, string gruop, out long nextOffset);
        IEnumerable<QueueKey> GetConsumeKeys();
        IEnumerable<TopicConsumeInfo> GetAllTopicConsumeInfoList();
        IEnumerable<TopicConsumeInfo> GetTopicConsumeInfoList(string groupName, string topic);
    }
}
