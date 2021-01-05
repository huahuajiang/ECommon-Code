using ECommon.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ECommon.Scheduling
{
    public class ScheduleService : IScheduleService
    {
        //readonly规则对值类型和引用类型有不同
        //值类型直接包含其数据，所以作为readonly值类型的字段 是不可变的
        //引用类型包含对其数据的readonly引用，所以作为引用类型的字段必须始终引用同一对象。该对象不是一成不变的
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, TimerBasedTask> _taskDict = new Dictionary<string, TimerBasedTask>();
        private readonly ILogger _logger;
        public ScheduleService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.Create(GetType().FullName);
        }

        public void StartTask(string name, Action action, int dueTime, int period)
        {
            lock (_lockObject)
            {
                if (_taskDict.ContainsKey(name)) return;
                //Timeout.Infinite:一个用于指定无限长等待时间的常数，适用于接受 Int32 参数的线程处理方法
                //用于指定无限长等待时间的常数，接受 TimeSpan 参数的方法
                var timer = new Timer(TaskCallback, name, Timeout.Infinite, Timeout.Infinite);
                _taskDict.Add(name, new TimerBasedTask { Name = name, Action = action, Timer = timer, DueTime = dueTime, Period = period, Stopped = false });
                //第一个参数意义是当Timer每一次触发执行回调前需要等待的时间，0表示立即触发，Infinite则表示永不触发回调；第二个参数表示每次触发timer的间隔时间，0表示只执行一次即第一次
                timer.Change(dueTime, period);
            }
        }

        public void StopTask(string name)
        {
            lock (_lockObject)
            {
                if (_taskDict.ContainsKey(name))
                {
                    var task = _taskDict[name];
                    task.Stopped = true;
                    task.Timer.Dispose();
                    _taskDict.Remove(name);
                }
            }
        }

        private void TaskCallback(object obj)
        {
            var taskName = (string)obj;
            TimerBasedTask task;

            if(_taskDict.TryGetValue(taskName,out task))
            {
                try
                {
                    if (!task.Stopped)
                    {
                        //取消Timer的重复性劳动等待下次启用
                        task.Timer.Change(Timeout.Infinite, Timeout.Infinite);
                        task.Action();
                    }
                }
                catch (ObjectDisposedException) { }
                catch(Exception ex)
                {
                    if (_logger != null)
                    {
                        _logger.Error(string.Format("Task has exception, name: {0}, due: {1}, period: {2}", task.Name, task.DueTime, task.Period), ex);
                    }
                }
                finally
                {
                    try
                    {
                        if (!task.Stopped)
                        {
                            task.Timer.Change(task.Period, task.Period);
                        }
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception ex)
                    {
                        if (_logger != null)
                        {
                            _logger.Error(string.Format("Timer change has exception, name: {0}, due: {1}, period: {2}", task.Name, task.DueTime, task.Period), ex);
                        }
                    }
                }
            }
        }
    }

    class TimerBasedTask
    {
        public string Name;
        public Action Action;
        public Timer Timer;
        public int DueTime;//预定时间
        public int Period;//间隔
        public bool Stopped;
    }
}
