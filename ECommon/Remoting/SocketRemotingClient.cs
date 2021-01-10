using ECommon.Logging;
using ECommon.Scheduling;
using ECommon.Socketing;
using ECommon.Socketing.BufferManagement;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ECommon.Remoting
{
    public class SocketRemotingClient
    {
        private readonly byte[] TimeoutMessage = Encoding.UTF8.GetBytes("Remoting request timeout.");
        private readonly Dictionary<int, IResponseHandler> _responseHandlerDict;
        private readonly Dictionary<int, IRemotingServerMessageHandler> _remotingServerMessageHandlerDict;
        private readonly IList<IConnectionEventListener> _connectionEventListeners;
        private readonly ConcurrentDictionary<long, ResponseFuture> _responseFutureDict;
        private readonly BlockingCollection<byte[]> _replyMessageQueue;
        private readonly IScheduleService _scheduleService;
        private readonly ILogger _logger;
        private readonly SocketSetting _setting;
        private readonly byte[] HeartbeatMessage = new byte[0];
        private int _reconnection = 0;
        private bool _shutteddown = false;
        private bool _started = false;

        public string Name { get; }
        public bool IsConnected
        {
            get { return ClientSocket != null && ClientSocket.IsConnected; }
        }
        public EndPoint LocalEndPoint { get; private set; }
        public EndPoint ServerEndPoint { get; }
        public ClientSocket ClientSocket { get; private set; }
        public IBufferPool BufferPool { get; }
    }
}
