using log4net;
using System;
using System.Collections.Generic;

using TrunkBot.Api;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class Notifier
    {
        internal static void NotifyException(
            RestApi restApi,
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

            NotifyTaskStatus(
                restApi, branch.Owner, exMessage,
                notificationsConfig);
        }

        internal static void NotifyTaskStatus(
            RestApi restApi,
            string owner,
            string message,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            if (notificationsConfig == null)
                return;

            try
            {
                List<string> recipients = GetNotificationRecipients(
                    restApi, owner, notificationsConfig);

                TrunkMergebotApi.Notify.Message(
                    restApi, notificationsConfig.Plug, message, recipients);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error notifying task status message '{0}'. Error: {1}",
                    message, e.Message);
                mLog.DebugFormat("StackTrace:{0}{1}", Environment.NewLine, e.StackTrace);
            }
        }

        static List<string> GetNotificationRecipients(
            RestApi restApi,
            string owner,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            List<string> recipients = new List<string>();
            recipients.Add(owner);

            if (notificationsConfig.FixedRecipients != null)
                recipients.AddRange(notificationsConfig.FixedRecipients);

            return ResolveUserProfile.ResolveFieldForUsers(
                restApi, recipients, notificationsConfig.UserProfileField);
        }

        static readonly ILog mLog = LogManager.GetLogger("notifier");
    }
}
