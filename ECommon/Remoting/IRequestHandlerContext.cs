using ECommon.Socketing;
using System;

namespace ECommon.Remoting
{
    /// <summary>
    /// 请求处理上下文
    /// </summary>
    public interface IRequestHandlerContext
    {
        ITcpConnection Connection { get; }

        Action<RemotingResponse> SendRemotingResponse { get; }
    }
}
