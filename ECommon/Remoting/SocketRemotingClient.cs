using ECommon.Components;
using ECommon.Logging;
using ECommon.Remoting.Exceptions;
using ECommon.Scheduling;
using ECommon.Socketing;
using ECommon.Socketing.BufferManagement;
using ECommon.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        private int _reconnecting = 0;
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

        public SocketRemotingClient(string name) : this(name, new IPEndPoint(IPAddress.Loopback, 5000)) { }

        public SocketRemotingClient(string name,EndPoint serverEndPoint, SocketSetting setting = null, EndPoint localEndPoint = null)
        {
            Ensure.NotNull(serverEndPoint, "serverEndPoint");

            Name = name;
            ServerEndPoint = serverEndPoint;
            LocalEndPoint = localEndPoint;
            _setting = setting ?? new SocketSetting();
            BufferPool = new BufferPool(_setting.ReceiveDataBufferSize, _setting.ReceiveDataBufferPoolSize);
            ClientSocket = new ClientSocket(name, ServerEndPoint, LocalEndPoint, _setting, BufferPool, HandleServerMessage);
            _responseFutureDict = new ConcurrentDictionary<long, ResponseFuture>();
            _replyMessageQueue = new BlockingCollection<byte[]>(new ConcurrentQueue<byte[]>());
            _responseHandlerDict = new Dictionary<int, IResponseHandler>();
            _remotingServerMessageHandlerDict = new Dictionary<int, IRemotingServerMessageHandler>();
            _connectionEventListeners = new List<IConnectionEventListener>();
            _scheduleService = ObjectContainer.Resolve<IScheduleService>();
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);

            RegisterConnectionEventListener(new ConnectionEventListener(this));
        }

        public SocketRemotingClient RegisterResponseHandler(int requestCode, IResponseHandler responseHandler)
        {
            _responseHandlerDict[requestCode] = responseHandler;
            return this;
        }

        public SocketRemotingClient RegisterConnectionEventListener(IConnectionEventListener listener)
        {
            _connectionEventListeners.Add(listener);
            ClientSocket.RegisterConnectionEventListener(listener);
            return this;
        }

        public SocketRemotingClient Start()
        {
            if (_started) return this;

            StartClientSocket();
            StartScanTimeoutRequestTask();
            _shutteddown = false;
            _started = true;
            return this;
        }

        public Task<RemotingResponse> InvokeAsync(RemotingRequest request, int timeoutMillis)
        {
            EnsureClientStatus();

            request.Type = RemotingRequestType.Async;
            var taskCompletionSource = new TaskCompletionSource<RemotingResponse>();
            var responseFuture = new ResponseFuture(request, timeoutMillis, taskCompletionSource);

            if (!_responseFutureDict.TryAdd(request.Sequence, responseFuture))
            {
                throw new ResponseFutureAddFailedException(request.Sequence);
            }

            ClientSocket.QueueMessage()
            
        }

        private void EnsureClientStatus()
        {
            if (ClientSocket == null || !ClientSocket.IsConnected)
            {
                throw new RemotingServerUnAvailableException(ServerEndPoint);
            }
        }
        private void ExitReconnecting()
        {
            Interlocked.Exchange(ref _reconnecting, 0);
        }
        private void SetLocalEndPoint(EndPoint localEndPoint)
        {
            LocalEndPoint = localEndPoint;
        }
    }
}
