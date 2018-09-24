using System;
using System.IO;

using log4net;

using Newtonsoft.Json.Linq;

namespace TrunkBot.Configuration
{
    internal class TrunkBotConfiguration
    {
        internal class PlasticSCM
        {
            internal readonly StatusProperty StatusAttribute;

            internal PlasticSCM (StatusProperty statusAttribute)
            {
                StatusAttribute = statusAttribute;
            }
        }

        internal class IssueTracker
        {
            internal readonly string Plug;
            internal readonly string ProjectKey;
            internal readonly string TitleField;
            internal readonly StatusProperty StatusField;

            internal IssueTracker(
                string plug,
                string projectKey,
                string titleField,
                StatusProperty statusField)
            {
                Plug = plug;
                ProjectKey = string.IsNullOrEmpty(projectKey)
                    ? "default_proj"
                    : projectKey;
                TitleField = titleField;
                StatusField = statusField;
            }
        }

        internal class ContinuousIntegration
        {
            internal readonly string Plug;
            internal readonly string Plan;

            internal ContinuousIntegration(string plug, string plan)
            {
                Plug = plug;
                Plan = plan;
            }
        }

        internal class Notifier
        {
            internal readonly string Plug;
            internal readonly string UserProfileField;
            internal readonly string[] FixedRecipients;

            internal Notifier(string plug, string userProfileField, string[] fixedRecipients)
            {
                Plug = plug;
                UserProfileField = userProfileField;
                FixedRecipients = fixedRecipients;
            }
        }

        internal class StatusProperty
        {
            internal readonly string Name;
            internal readonly string ResolvedValue;
            internal readonly string TestingValue;
            internal readonly string FailedValue;
            internal readonly string MergedValue;

            internal StatusProperty(
                string name,
                string resolvedValue,
                string testingValue,
                string failedValue,
                string mergedValue)
            {
                Name = name;
                ResolvedValue = resolvedValue;
                TestingValue = testingValue;
                FailedValue = failedValue;
                MergedValue = mergedValue;
            }
        }

        internal readonly string Server;
        internal readonly string Repository;
        internal readonly string TrunkBranch;
        internal readonly string BranchPrefix;
        internal readonly string UserApiKey;
        internal readonly PlasticSCM Plastic;
        internal readonly IssueTracker Issues;
        internal readonly ContinuousIntegration CI;
        internal readonly Notifier Notifications;

        internal static TrunkBotConfiguration BuidFromConfigFile(string configFile)
        {
            try
            {
                string fileContent = File.ReadAllText(configFile);
                JObject jsonObject = JObject.Parse(fileContent);

                if (jsonObject == null)
                    return null;

                return new TrunkBotConfiguration(
                    GetPropertyValue(jsonObject, "server"),
                    GetPropertyValue(jsonObject, "repository"),
                    GetPropertyValue(jsonObject, "trunk_branch"),
                    GetPropertyValue(jsonObject, "branch_prefix"),
                    GetPropertyValue(jsonObject, "bot_user"),
                    BuildPlasticSCM(jsonObject["plastic_group"]),
                    BuildIssueTracker(jsonObject["issues_group"]),
                    BuildContinuousIntegration(jsonObject["ci_group"]),
                    BuildNotifier(jsonObject["notifier_group"]));
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Trunkbot configuration cannot be read from '{0}' : {1}",
                    configFile, ex.Message);
                mLog.DebugFormat("StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }

            return null;
        }

        static PlasticSCM BuildPlasticSCM(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            return new PlasticSCM(
                BuildStatusProperty(jsonToken["status_attribute_group"]));
        }

        static IssueTracker BuildIssueTracker(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            string plug = GetPropertyValue(jsonToken, "plug");

            if (string.IsNullOrEmpty(plug))
                return null;

            return new IssueTracker(
                plug,
                GetPropertyValue(jsonToken, "project_key"),
                GetPropertyValue(jsonToken, "title_field"),
                BuildStatusProperty(jsonToken["status_field_group"]));
        }

        static ContinuousIntegration BuildContinuousIntegration(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            return new ContinuousIntegration(
                GetPropertyValue(jsonToken, "plug"),
                GetPropertyValue(jsonToken, "plan"));
        }

        static Notifier BuildNotifier(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            string plug = GetPropertyValue(jsonToken, "plug");

            if (string.IsNullOrEmpty(plug))
                return null;

            return new Notifier(
                plug,
                GetPropertyValue(jsonToken, "user_profile_field"),
                GetFixedRecipientsArray(GetPropertyValue(jsonToken, "fixed_recipients")));
        }

        static StatusProperty BuildStatusProperty(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            return new StatusProperty(
                GetPropertyValue(jsonToken, "name"),
                GetPropertyValue(jsonToken, "resolved_value"),
                GetPropertyValue(jsonToken, "testing_value"),
                GetPropertyValue(jsonToken, "failed_value"),
                GetPropertyValue(jsonToken, "merged_value"));
        }

        static string[] GetFixedRecipientsArray(string fixedRecipients)
        {
            if (fixedRecipients == null)
                return null;

            string[] result = fixedRecipients.Split(
                new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        static string GetPropertyValue(JToken jsonToken, string key)
        {
            JToken jsonProperty = jsonToken[key];

            if (jsonProperty == null)
                return null;

            return jsonProperty.Value<string>();
        }

        TrunkBotConfiguration(
            string server,
            string repository,
            string trunkBranch,
            string branchPrefix,
            string userApiKey,
            PlasticSCM plastic,
            IssueTracker issues,
            ContinuousIntegration ci,
            Notifier notifications)
        {
            Server = server;
            Repository = repository;
            TrunkBranch = trunkBranch;
            BranchPrefix = branchPrefix;
            UserApiKey = userApiKey;
            Plastic = plastic;
            Issues = issues;
            CI = ci;
            Notifications = notifications;
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBotConfiguration");
    }
}
