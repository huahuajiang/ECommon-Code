using ECommon.Logging;
using ECommon.Scheduling;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ECommon.Utilities
{
    public class DefaultPerformanceService : IPerformanceService
    {
        private string _name;
        private PerformanceServiceSetting _setting;
        private string _taskName;

        private readonly ILogger _logger;
        private readonly IScheduleService _scheduleService;
        private readonly ConcurrentDictionary<string, CountInfo> _countInfoDict;

        public string Name
        {
            get { return _name; }
        }

        public PerformanceServiceSetting Setting
        {
            get { return _setting; }
        }

        public DefaultPerformanceService(IScheduleService scheduleService,ILoggerFactory loggerFactory)
        {
            _scheduleService = scheduleService;
            _logger = loggerFactory.Create(GetType().FullName);
            _countInfoDict = new ConcurrentDictionary<string, CountInfo>();
        }

        public PerformanceInfo GetKeyPerformanceInfo(string key)
        {
            throw new NotImplementedException();
        }

        public void IncrementKeyCount(string key, double rtMilliseconds)
        {
            throw new NotImplementedException();
        }

        public IPerformanceService Initialize(string name, PerformanceServiceSetting setting = null)
        {
            Ensure.NotNullOrEmpty(name, "name");

            if (setting == null)
            {
                _setting = new PerformanceServiceSetting
                {
                    AutoLogging = true,
                    StatIntervalSeconds = 1
                };
            }
            else
            {
                _setting = setting;
            }

            Ensure.Positive(_setting.StatIntervalSeconds, "PerformanceServiceSetting.StatIntervalSeconds");

            _name = name;
            _taskName = name + ".Task";

            return this;
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }

        public void UpdateKeyCount(string key, long count, double rtMilliseconds)
        {
            throw new NotImplementedException();
        }


        class CountInfo
        {
            private DefaultPerformanceService _service;

            private long _totalCount;
            private long _previousCount;
            private long _throughput;
            private long _averageThroughput;
            private long _throughputCalculateCount;

            private long _rtCount;
            private long _totalRTTime;
            private long _rtTime;
            private double _rt;
            private double _averateRT;
            private long _rtCalculateCount;

            public long TotalCount
            {
                get { return _totalCount; }
            }
            public long Throughput
            {
                get { return _throughput; }
            }
            public long AverageThroughput
            {
                get { return _averageThroughput; }
            }
            public double RT
            {
                get { return _rt; }
            }
            public double AverageRT
            {
                get { return _averateRT; }
            }

            public CountInfo(DefaultPerformanceService service,long initialCount,double rtMilliseconds)
            {
                _service = service;
                _totalCount = initialCount;
                _rtCount = initialCount;
                //Interlocked:实现进程 的同步
                Interlocked.Add(ref _rtTime, (long)(rtMilliseconds * 1000));
                Interlocked.Add(ref _totalRTTime, (long)(rtMilliseconds * 1000));
            }

            public void IncrementTotalCount(double rtMilliseconds)
            {
                Interlocked.Increment(ref _totalCount);//+1
                Interlocked.Increment(ref _rtCount);
                Interlocked.Add(ref _rtTime, (long)(rtMilliseconds * 1000));
                Interlocked.Add(ref _totalRTTime, (long)(rtMilliseconds * 1000));
            }

            public void UpdateTotalCount(long count,double rtMilliseconds)
            {
                _totalCount = count;
                _rtCount = count;
                Interlocked.Add(ref _rtTime, (long)(rtMilliseconds * 1000));
                Interlocked.Add(ref _totalRTTime, (long)(rtMilliseconds * 1000));
            }
        }
    }
}
