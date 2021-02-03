using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Protocols.Brokers
{
    [Serializable]
    public class QueueKey : IComparable<QueueKey>, IComparable
    {
        public string Topic { get; set; }
        public int QueueId { get; set; }
        public QueueKey() { }

        public QueueKey(string topic,int queueId)
        {
            Topic = topic;
            QueueId = queueId;
        }

        public static bool operator ==(QueueKey left, QueueKey right)
        {
            return IsEqual(left, right);
        }

        public static bool operator !=(QueueKey left, QueueKey right) {
            return !IsEqual(left, right);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }


        }

        public int CompareTo(object obj)
        {
            return ToString().CompareTo(obj.ToString());
        }

        public int CompareTo(QueueKey other)
        {
            return ToString().CompareTo(other.ToString());
        }
    }
}
