using ECommon.Components;
using ECommon.Logging;
using ECommon.Socketing;
using ECommon.Socketing.BufferManagement;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ECommon.Remoting
{
    public class SocketRemotingServer
    {
        private readonly Dictionary<int, IRequestHandler> _requestHandlerDict;
        private readonly ILogger _logger;
        private readonly SocketSetting _setting;
        private bool _isShuttingdown = false;
        private readonly byte[] HeartbeatResponseMessage = new byte[0];

        public string Name { get; }
        public IBufferPool BufferPool { get; }
        public ServerSocket ServerSocket { get; }

        public SocketRemotingServer() : this("Server", new IPEndPoint(IPAddress.Loopback, 5000)) { }

        public SocketRemotingServer(string name,IPEndPoint listeningEndPoint, SocketSetting setting = null)
        {
            Name = name;
            _setting = setting ?? new SocketSetting();
            BufferPool = new BufferPool(_setting.ReceiveDataBufferSize, _setting.ReceiveDataBufferPoolSize);
            ServerSocket = new ServerSocket(name, listeningEndPoint, _setting, BufferPool, HandleRemotingRequest);
            _requestHandlerDict = new Dictionary<int, IRequestHandler>();
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(name ?? GetType().Name);
        }

        private void HandleRemotingRequest(ITcpConnection connection, byte[] message, Action<byte[]> sendReplyAction)
        {
            if (_isShuttingdown) return;

            var remotingRequest = RemotingUtil.ParseRequest(message);
            var requestHandlerContext=new SocketRequestHandlerContext(connection, sendReplyAction);

            if (!_requestHandlerDict.TryGetValue(remotingRequest.Code, out IRequestHandler requestHandler))
            {
                var errorMessage = string.Format("No request handler found for remoting request, remotingServerName: {0}, remotingRequest: {1}", Name, remotingRequest);
                _logger.Error(errorMessage);
                if (remotingRequest.Type != RemotingRequestType.Oneway)
                {
                    requestHandlerContext.SendRemotingResponse(new RemotingResponse(
                        remotingRequest.Type,
                        remotingRequest.Code,
                        remotingRequest.Sequence,
                        remotingRequest.CreatedTime,
                        -1,
                        Encoding.UTF8.GetBytes(errorMessage),
                        DateTime.Now,
                        remotingRequest.Header,
                        null));
                }
                return;
            }

            try
            {
                var remotingResponse = requestHandler.HandleRequest(requestHandlerContext, remotingRequest);

            }
        }
    }
}
