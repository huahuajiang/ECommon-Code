using EQueue.Protocols.Brokers;
using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Broker
{
    public interface IQueueStore
    {
        void Load();
        void Start();
        void Shutdown();
        IEnumerable<string> GetAllTopics();
        Queue GetQueue(string topic, int queueuId);
        int GetAllQueueCount();
        IEnumerable<Queue> GetAllQueues();
        IList<TopicQueueInfo> GetTopicQueueInfoList(string topic = null);
        long GetTotalUnConusmedMessageCount();
        bool IsTopicExist(string topic);
        bool IsQueueExist(QueueKey queueKey);
        bool IsQueueExist(string topic, int queueId);
        long GetQueueCurrentOffset(string topic, int queueId);
        long GetQueueMinOffset(string topic, int queueId);
        void AddQueue(string topic);
        void DeleteQueue(string topic, int queueId);
        void SetProducerVisible()
    }
}
