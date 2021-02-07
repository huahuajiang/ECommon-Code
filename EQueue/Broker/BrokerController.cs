using ECommon.Logging;
using ECommon.Scheduling;
using ECommon.Serializing;
using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Broker
{
    public class BrokerController
    {
        private static BrokerController _instance;
        private readonly ILogger _logger;
        private readonly IQueueStore _queueStroe;
        private readonly IConsumeOffsetStore _consumeOffsetStore;
        private readonly IBinarySerializer _binarySerializer;
        private readonly IScheduleService _scheduleService;
        private readonly 
    }
}
