using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ECommon.Scheduling
{
    public class ScheduleService : IScheduleService
    {
        private readonly object _lockObject = new object();
        private readonly Dictionary<string, TimerBasedTask> _taskDice = new Dictionary<string, TimerBasedTask>();
        public void StartTask(string name, Action action, int dueTime, int period)
        {
            throw new NotImplementedException();
        }

        public void StopTask(string name)
        {
            throw new NotImplementedException();
        }
    }

    class TimerBasedTask
    {
        public string Name;
        public Action Action;
        public Timer Timer;
        public int DueTime;
        public int Period;
        public bool Stopped;
    }
}
