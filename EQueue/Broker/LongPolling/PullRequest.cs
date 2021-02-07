using ECommon.Remoting;
using EQueue.Broker.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace EQueue.Broker.LongPolling
{
    public class PullRequest
    {
        public RemotingRequest RemotingRequest { get; private set; }
        public PullMessageRequest PullMessageRequest { get; private set; }
        public IRequestHandlerContext RequesthandlerContext { get; private set; }
        public DateTime SuspendStartTime { get; private set; }
        public long SuspendMilliseconds { get; private set; }
        public Action<PullRequest> NewMessageArrivedAction { get; private set; }
        public Action<PullRequest> TimeoutAction { get; private set; }
        public Action<PullRequest> NoNewMessageAction { get; private set; }
        public Action<PullRequest> ReplacedAction { get; private set; }

        public PullRequest(
            RemotingRequest remotingRequest,
            PullMessageRequest pullMessageRequest,
            IRequestHandlerContext requestHandlerContext,
            DateTime suspendStartTime,
            long suspendMilliseconds,
            Action<PullRequest> newMessageArrivedAction,
            Action<PullRequest> timeoutAction,
            Action<PullRequest> noNewMessageAction,
            Action<PullRequest> replacedAction)
        {
            RemotingRequest = remotingRequest;
            PullMessageRequest = pullMessageRequest;
            RequesthandlerContext = requestHandlerContext;
            SuspendStartTime = suspendStartTime;
            SuspendMilliseconds = suspendMilliseconds;
            NewMessageArrivedAction = newMessageArrivedAction;
            TimeoutAction = timeoutAction;
            NoNewMessageAction = noNewMessageAction;
            ReplacedAction = replacedAction;
        }

        public bool IsTimeout()
        {
            return (DateTime.Now - SuspendStartTime).TotalMilliseconds >= SuspendMilliseconds;
        }
    }
}
