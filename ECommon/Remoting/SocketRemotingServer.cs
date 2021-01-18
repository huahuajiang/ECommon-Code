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
    /// <summary>
    /// 服务端
    /// </summary>
    public class SocketRemotingServer
    {
        private readonly Dictionary<int, IRequestHandler> _requestHandlerDict;//请求时的处理事件
        private readonly ILogger _logger;
        private readonly SocketSetting _setting;
        private bool _isShuttingdown = false;//是否停止
        private readonly byte[] HeartbeatResponseMessage = new byte[0];

        public string Name { get; }
        public IBufferPool BufferPool { get; }
        public ServerSocket ServerSocket { get; }//服务端的socket

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

        public SocketRemotingServer RegisterConnectionEventListener(IConnectionEventListener listener)
        {
            ServerSocket.RegisterConnectionEventListener(listener);
            return this;
        }
        public SocketRemotingServer Start()
        {
            _isShuttingdown = false;
            ServerSocket.Start();
            return this;
        }
        public SocketRemotingServer Shutdown()
        {
            _isShuttingdown = true;
            ServerSocket.Shutdown();
            return this;
        }
        public SocketRemotingServer RegisterRequestHandler(int requestCode, IRequestHandler requestHandler)
        {
            _requestHandlerDict[requestCode] = requestHandler;
            return this;
        }
        public void PushMessageToAllConnections(RemotingServerMessage message)
        {
            var data = RemotingUtil.BuildRemotingServerMessage(message);
            ServerSocket.PushMessageToAllConnections(data);
        }
        public void PushMessageToConnection(Guid connectionId, byte[] message)
        {
            ServerSocket.PushMessageToConnection(connectionId, message);
        }
        public IList<ITcpConnection> GetAllConnections()
        {
            return ServerSocket.GetAllConnections();
        }

        /// <summary>
        /// 处理远程请求的事件
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="message"></param>
        /// <param name="sendReplyAction"></param>
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
                if (remotingRequest.Type != RemotingRequestType.Oneway && remotingResponse != null)
                {
                    requestHandlerContext.SendRemotingResponse(remotingResponse);
                }
            }
            catch (Exception ex)
            {
                var errorMessage = string.Format("Unknown exception raised when handling remoting request:{0}, name: {0}, request: {1}", Name, remotingRequest);
                _logger.Error(errorMessage, ex);
                if (remotingRequest.Type != RemotingRequestType.Oneway)
                {
                    requestHandlerContext.SendRemotingResponse(new RemotingResponse(
                        remotingRequest.Type,
                        remotingRequest.Code,
                        remotingRequest.Sequence,
                        remotingRequest.CreatedTime,
                        -1,
                        Encoding.UTF8.GetBytes(ex.Message),
                        DateTime.Now,
                        remotingRequest.Header,
                        null));
                }
            }
        }
    }
}
