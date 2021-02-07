using ECommon.Components;
using ECommon.Logging;
using ECommon.Scheduling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EQueue.Broker.LongPolling
{
    /// <summary>
    /// Supended:暂停
    /// </summary>
    class SuspendedPullRequestManager
    {
        #region Private Variables

        private const string Separator = "@";
        private readonly BlockingCollection<NotifyItem> _notifyQueue = new BlockingCollection<NotifyItem>();
        private readonly ConcurrentDictionary<string, PullRequest> _queueRequestDict = new ConcurrentDictionary<string, PullRequest>();
        private readonly IScheduleService _scheduleService;
        private readonly IQueueStore _queueStore;
        private readonly ILogger _logger;
        private readonly Worker _notifyMessageArrivedWorker;

        #endregion

        public SuspendedPullRequestManager()
        {
            _scheduleService = ObjectContainer.Resolve<IScheduleService>();
            _queueStore = ObjectContainer.Resolve<IQueueStore>();
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);
            _notifyMessageArrivedWorker = new Worker("NotifyMessageArrived", () =>
              {
                  var notifyItem = _notifyQueue.Take();
                  if (notifyItem == null) return;
                  NotifyMessageArrived(notifyItem.Topic, notifyItem.QueueId, notifyItem.QueueOffset);
              });
        }

        public void Clean()
        {
            var keys = _queueRequestDict.Keys.ToList();
            foreach(var key in keys)
            {
                PullRequest request;
                if(_queueRequestDict.TryRemove(key,out request))
                {
                    Task.Factory.StartNew(() => request.NoNewMessageAction(request));
                }
            }
        }

        class NotifyItem
        {
            public string Topic { get; set; }
            public int QueueId { get; set; }
            public long QueueOffset { get; set; }
        }
    }
}
