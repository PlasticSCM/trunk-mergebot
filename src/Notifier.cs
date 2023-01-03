using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Codice.LogWrapper;
using Codice.CM.Server.Devops;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class Notifier
    {
       internal static async Task NotifyException(
            INotifierPlugService notifier,
            IGetUserProfile userProfile,
            Branch branch,
            string message,
            string taskStatus,
            Exception exception,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            string exMessage = string.Format(
                "There was an error setting the branch '{0}' as '{1}'. " +
                "Error: {2}. Inner error: {3}",
                branch.FullName, taskStatus, exception.Message, message);

            await NotifyTaskStatus(
                notifier, userProfile, branch.Owner, exMessage,
                notificationsConfig);
        }

        internal static async Task NotifyTaskStatus(
            INotifierPlugService notifier,
            IGetUserProfile userProfile,
            string owner,
            string message,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            if (notificationsConfig == null)
                return;

            try
            {
                List<string> recipients = GetNotificationRecipients(
                    userProfile, owner, notificationsConfig);

                if (recipients == null)
                    return;
                
                await notifier.NotifyMessage(notificationsConfig.Plug, message, recipients);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error notifying task status message '{0}'. Error: {1}",
                    message, e.Message);
                mLog.DebugFormat("StackTrace:{0}{1}", Environment.NewLine, e.StackTrace);
            }
        }

        static List<string> GetNotificationRecipients(
            IGetUserProfile userProfile,
            string owner,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            List<string> recipients = new List<string>();

            if (!string.IsNullOrWhiteSpace(owner))
                recipients.Add(owner);

            if (notificationsConfig.FixedRecipients != null)
                recipients.AddRange(notificationsConfig.FixedRecipients);

            if (string.IsNullOrEmpty(notificationsConfig.UserProfileField))
                return recipients;

            return ResolveUserProfile.ResolveFieldForUsers(
                userProfile, recipients, notificationsConfig.UserProfileField);
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-Notifier");
    }
}
