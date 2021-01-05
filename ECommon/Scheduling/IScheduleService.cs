using System;
using System.Collections.Generic;
using System.Text;

namespace ECommon.Scheduling
{
    public interface IScheduleService
    {
        void StartTask(string name, Action action, int dueTime, int period);
        void StopTask(string name);
    }
}
