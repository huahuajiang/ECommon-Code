namespace ECommon.Remoting
{
    /// <summary>
    /// 
    /// </summary>
    public interface IRemotingServerMessageHandler
    {
        void HandleMessage(RemotingServerMessage message);
    }
}
