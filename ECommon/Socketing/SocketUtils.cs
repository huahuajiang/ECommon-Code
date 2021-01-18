using ECommon.Utilities;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace ECommon.Socketing
{
    public class SocketUtils
    {
        public static IPAddress GetLovalIPV4()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);
        }

        public static Socket CreateSocket(int sendBufferSize,int receiveBufferSize)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
                Blocking = false,//阻塞
                SendBufferSize = sendBufferSize,//发送消息的大小
                ReceiveBufferSize = receiveBufferSize//接收消息的大小
            };
            return socket;
        }

        public static void ShutdownSocket(Socket socket)
        {
            if (socket == null) return;

            Helper.EatException(() => socket.Shutdown(SocketShutdown.Both));
            Helper.EatException(() => socket.Close(10000));
        }

        public static void CloseSocket(Socket socket)
        {
            if (socket == null) return;

            Helper.EatException(() => socket.Close(10000));
        }
    }
}
