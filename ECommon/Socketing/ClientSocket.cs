using ECommon.Components;
using ECommon.Logging;
using ECommon.Socketing.BufferManagement;
using ECommon.Utilities;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ECommon.Socketing
{
    public class ClientSocket
    {
        private readonly EndPoint _serverEndPoint;
        private readonly EndPoint _localEndPoint;
        private Socket _socket;
        private readonly SocketSetting _setting;
        private readonly IList<IConnectionEventListener> _connectionEventListeners;
        private readonly Action<ITcpConnection, byte[]> _messageArrivedHandler;
        private readonly IBufferPool _receiveDataBufferPool;
        private readonly ILogger _logger;
        private readonly ManualResetEvent _waitConnectHandle;
        private readonly int _flowControlThreshold;
        private long _flowControlTimes;


        public bool IsConnected
        {
            get { return Connection != null&& Connection.IsConnected; }
        }

        public string Name { get; private set; }
        public TcpConnection Connection { get; private set; }

        public ClientSocket(string name, EndPoint serverEndPoint, EndPoint localEndPoint, SocketSetting setting, IBufferPool receiveDataBufferPool, Action<ITcpConnection, byte[]> messageArrivedHandler)
        {
            Ensure.NotNull(serverEndPoint, "serverEndPoint");
            Ensure.NotNull(setting, "setting");
            Ensure.NotNull(receiveDataBufferPool, "receiveDataBufferPool");
            Ensure.NotNull(messageArrivedHandler, "messageArrivedHandler");

            Name = name;
            _connectionEventListeners = new List<IConnectionEventListener>();
            _serverEndPoint = serverEndPoint;
            _localEndPoint = localEndPoint;
            _setting = setting;
            _receiveDataBufferPool = receiveDataBufferPool;
            _messageArrivedHandler = messageArrivedHandler;
            _waitConnectHandle = new ManualResetEvent(false);
            _socket = SocketUtils.CreateSocket(_setting.SendBufferSize, _setting.ReceiveBufferSize);
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);
            _flowControlThreshold = _setting.SendMessageFlowControlThreshold;
        }

        public ClientSocket RegisterConnectionEventListener(IConnectionEventListener listener)
        {
            _connectionEventListeners.Add(listener);
            return this;
        }


    }
}
