using ECommon.Components;
using ECommon.Logging;
using System;
using System.Threading;

namespace ECommon.Scheduling
{
    /// <summary>
    /// 表示将重复执行特定方法的后台工作进程
    /// </summary>
    public class Worker
    {
        private readonly object _lockObject = new object();
        private readonly string _actionName;
        private readonly Action _action;
        private readonly ILogger _logger;
        private Status _status;

        public string ActionName
        {
            get { return _actionName; }
        }

        public Worker(string actionName,Action action)
        {
            _actionName = actionName;
            _action = action;
            _status = Status.Initial;
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);
        }

        public Worker Start()
        {
            lock (_lockObject)
            {
                if (_status == Status.Running) return this;

                _status = Status.Running;
                new Thread(Loop)
                {
                    Name = string.Format("{0}.Worker", _actionName),
                    IsBackground = true
                }.Start(this);

                return this;
            }
        }

        public Worker Stop()
        {
            lock (_lockObject)
            {
                if (_status == Status.StopRequested) return this;
                _status = Status.StopRequested;
                return this;
            }
        }

        private void Loop(object data)
        {
            var worker = (Worker)data;

            while (worker._status == Status.Running)
            {
                try { _action(); }
                catch (ThreadAbortException)
                {
                    _logger.InfoFormat("Worker thread caught ThreadAbortException, try to resetting, actionName:{0}", _actionName);
                    Thread.ResetAbort();
                    _logger.InfoFormat("Worker thread ThreadAbortException resetted, actionName:{0}", _actionName);
                }
                catch (Exception ex)
                {
                    _logger.Error(string.Format("Worker thread has exception, actionName:{0}", _actionName), ex);
                }
            }
        }
    }

    enum Status
    {
        Initial,
        Running,
        StopRequested
    }
}
