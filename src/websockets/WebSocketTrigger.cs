using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using Newtonsoft.Json.Linq;
using TrunkBot.Messages;

namespace TrunkBot.WebSockets
{
    public class WebSocketTrigger
    {
        internal WebSocketTrigger(INotifyMergebotTriggerActions triggerActions)
        {
            mTriggerActions = triggerActions;
        }

        internal void OnEventReceived(object state)
        {
            //No new events are received while this event is processed so avoid process it here
            string message = (string) state;

            mLog.Debug(message);

            if (IsBranchAttributeChangedEvent(message))
            {
                BranchAttributeChangeEvent e =
                    ParseEvent.Parse<BranchAttributeChangeEvent>(message);

                mTriggerActions.NotifyBranchAttributeChanged(
                    e.Repository,
                    int.Parse(e.BranchId),
                    e.BranchFullName,
                    e.AttributeName,
                    e.AttributeValue,
                    e.BranchOwner,
                    e.BranchComment);

                return;
            }

            if (IsCodeReviewChangedEvent(message))
            {
                CodeReviewChangeEvent e = ParseEvent.Parse<CodeReviewChangeEvent>(message);
                mTriggerActions.NotifyCodeReviewStatusChanged(
                    e.Repository,
                    int.Parse(e.BranchId),
                    e.BranchFullName,
                    e.BranchOwner,
                    e.BranchComment,
                    int.Parse(e.CodeReviewId),
                    e.CodeReviewTitle,
                    e.CodeReviewStatus);
                return;
            }
            
            // event discarded, nothing to process.
        }

        bool IsBranchAttributeChangedEvent(string message)
        {
            return GetEventTypeFromMessage(message).Equals(
                WebSocketClient.BRANCH_ATTRIBUTE_CHANGED_TRIGGER_TYPE);
        }

        bool IsCodeReviewChangedEvent(string message)
        {
            return GetEventTypeFromMessage(message).Equals(
                WebSocketClient.CODE_REVIEW_CHANGED_TRIGGER_TYPE);
        }

        static string GetEventTypeFromMessage(string message)
        {
            try
            {
                JObject obj = JObject.Parse(message);
                return obj.Value<string>("event").ToString();
            }
            catch
            {
                mLog.ErrorFormat("Unable to parse incoming event: {0}", message);
                return string.Empty;
            }
        }

        readonly INotifyMergebotTriggerActions mTriggerActions;
        
        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-WebSocketTrigger");
    }
}