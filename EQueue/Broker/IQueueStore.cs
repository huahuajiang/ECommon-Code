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
        Queue
    }
}
