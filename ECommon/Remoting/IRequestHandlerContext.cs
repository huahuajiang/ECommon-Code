using ECommon.Socketing;
using System;

namespace ECommon.Remoting
{
    public interface IRequestHandlerContext
    {
        ITcpConnection Connection { get; }

        Action<RemotingResponse> SendRemotingResponse { get; }
    }
}
