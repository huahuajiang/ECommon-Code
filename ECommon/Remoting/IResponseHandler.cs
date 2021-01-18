namespace ECommon.Remoting
{
    /// <summary>
    /// 返回处理
    /// </summary>
    public interface IResponseHandler
    {
        void HandleResponse(RemotingResponse remotingResponse);
    }
}
