using ECommon.Components;
using ECommon.Extensions;
using ECommon.Logging;
using ECommon.Socketing.BufferManagement;
using ECommon.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ECommon.Socketing
{
    /// <summary>
    /// 服务端socket
    /// </summary>
    public class ServerSocket
    {
        private readonly Socket _socket;
        private readonly SocketSetting _setting;
        private readonly IPEndPoint _listeningEndPoint;
        private readonly SocketAsyncEventArgs _acceptSocketArgs;
        private readonly IList<IConnectionEventListener> _connectionEventListeners;
        private readonly Action<ITcpConnection, byte[], Action<byte[]>> _messageArrivedHandler;
        private readonly IBufferPool _receiveDataBufferPool;
        private readonly ConcurrentDictionary<Guid, ITcpConnection> _connectionDict;//所有的TcpConnection
        private readonly ILogger _logger;
        private readonly string _name;

        public ServerSocket(string name,IPEndPoint listeningEndPoint,SocketSetting setting,IBufferPool receiveDataBufferPool,Action<ITcpConnection,byte[],Action<byte[]>> messageArrivedHandler)
        {
            Ensure.NotNull(listeningEndPoint, "listeningEndPoint");
            Ensure.NotNull(setting, "setting");
            Ensure.NotNull(receiveDataBufferPool, "receiveDataBufferPool");
            Ensure.NotNull(messageArrivedHandler, "messageArrivedHandler");

            _name = name;//名称
            _listeningEndPoint = listeningEndPoint;//本地IP地址
            _setting = setting;
            _receiveDataBufferPool = receiveDataBufferPool;
            _connectionEventListeners = new List<IConnectionEventListener>();
            _messageArrivedHandler = messageArrivedHandler;
            _connectionDict = new ConcurrentDictionary<Guid, ITcpConnection>();
            _socket = SocketUtils.CreateSocket(_setting.SendBufferSize, _setting.ReceiveBufferSize);//创建socket
            _acceptSocketArgs = new SocketAsyncEventArgs();
            _acceptSocketArgs.Completed += AcceptCompleted;
            _logger = ObjectContainer.Resolve<ILoggerFactory>().Create(GetType().FullName);
        }

        public void RegisterConnectionEventListener(IConnectionEventListener listener)
        {
            _connectionEventListeners.Add(listener);
        }

        public void Start()
        {
            _logger.InfoFormat("Socket server is starting, name: {0}, listening on listeningEndPoint: {1}.", _name, _listeningEndPoint);

            try
            {
                _socket.Bind(_listeningEndPoint);
                _socket.Listen(5000);
            }
            catch(Exception ex)
            {
                _logger.Error(string.Format("Socket server start failed, name: {0}, listeningEndPoint: {1}.", _name, _listeningEndPoint), ex);
                SocketUtils.ShutdownSocket(_socket);
                throw;
            }

            StartAccepting();
        }

        public void Shutdown()
        {
            SocketUtils.ShutdownSocket(_socket);
            _logger.InfoFormat("Socket server shutdown, name: {0}, listeningEndPoint: {1}.", _name, _listeningEndPoint);
        }

        public void PushMessageToAllConnections(byte[] message)
        {
            foreach(var connection in _connectionDict.Values)
            {
                connection.QueueMessage(message);
            }
        }

        public void PushMessageToConnection(Guid connectionId, byte[] message)
        {
            ITcpConnection connection;
            if (_connectionDict.TryGetValue(connectionId, out connection))
            {
                connection.QueueMessage(message);
            }
        }

        public IList<ITcpConnection> GetAllConnections()
        {
            return _connectionDict.Values.ToList();
        }

        private void StartAccepting()
        {
            try
            {
                //如果 I/O 操作挂起，则为 true。 操作完成时，将引发 e 参数的 Completed 事件。
                //如果 I/O 操作同步完成，则为 false。 将不会引发 e 参数的 Completed 事件，并且可能在方法调用返回后立即检查作为参数传递的 e 对象以检索操作的结果。
                var firedAsynce = _socket.AcceptAsync(_acceptSocketArgs); //开始异步操作以接受传入的连接尝试
                if (!firedAsynce)
                {
                    ProcessAccept(_acceptSocketArgs);
                }
            }
            catch(Exception ex)
            {
                if(!(ex is ObjectDisposedException))
                {
                    _logger.Error(string.Format("Socket server accept has exception, name: {0}, listeningEndPoint: {1}.", _name, _listeningEndPoint), ex);
                }
                Task.Factory.StartNew(() => { StartAccepting(); });
            }
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        //接受完毕 开始接收和发送
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    var acceptSocket = e.AcceptSocket;
                    e.AcceptSocket = null;
                    OnSocketAccepted(acceptSocket, e.UserToken);
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _logger.Error(string.Format("Socket server process accept has exception, name: {0}, listeningEndPoint: {1}.", _name, _listeningEndPoint), ex);
            }
        }

        private void OnSocketAccepted(Socket socket,object userToken)
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    //
                    var connection = new TcpConnection(_name, socket, _setting, _receiveDataBufferPool, OnMessageArrived, OnConnectionClosed);
                    if (_connectionDict.TryAdd(connection.Id, connection))
                    {
                        _logger.InfoFormat("Socket server new client accepted, name: {0}, listeningEndPoint: {1}, remoteEndPoint: {2}", _name, _listeningEndPoint, socket.RemoteEndPoint);

                        foreach(var listener in _connectionEventListeners)
                        {
                            try
                            {
                                listener.OnConnectionAccepted(connection);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(string.Format("Notify socket server new client connection accepted has exception, name: {0}, listenerType: {1}", _name, listener.GetType().Name), ex);
                            }
                        }
                    }
                }
                catch (ObjectDisposedException) { }
                catch (Exception ex)
                {
                    _logger.Info(string.Format("Socket server accept new client has unknown exception, name: {0}, listeningEndPoint: {1}", _name, _listeningEndPoint), ex);
                }
            });
        }

        private void OnConnectionClosed(ITcpConnection connection, SocketError socketError)
        {
            _connectionDict.Remove(connection.Id);
            foreach(var listener in _connectionEventListeners)
            {
                try
                {
                    listener.OnConnectionClosed(connection, socketError);
                }
                catch(Exception ex)
                {
                    _logger.Error(string.Format("Notify socket server client connection closed has exception, name: {0}, listenerType: {1}", _name, listener.GetType().Name), ex);
                }
            }
        }

        private void OnMessageArrived(ITcpConnection connection, byte[] message)
        {
            try
            {
                _messageArrivedHandler(connection, message, reply =>
                {
                    Task.Factory.StartNew(() => connection.QueueMessage(reply));
                });
            }
            catch(Exception ex)
            {
                _logger.Error(string.Format("Socket server handle message has exception, name: {0}, listeningEndPoint: {1}", _name, _listeningEndPoint), ex);
            }
        }
    }
}
