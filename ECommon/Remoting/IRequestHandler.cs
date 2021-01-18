namespace ECommon.Remoting
{
    /// <summary>
    /// 请求处理
    /// </summary>
    public interface IRequestHandler
    {
        RemotingResponse HandleRequest(IRequestHandlerContext context, RemotingRequest remotingRequest);
    }
}
