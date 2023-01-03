using System;
using System.IO;

using Codice.LogWrapper;

using Newtonsoft.Json.Linq;

namespace TrunkBot.Configuration
{
    public class TrunkBotConfiguration
    {
        public class PlasticSCM
        {
            public readonly bool IsApprovedCodeReviewFilterEnabled;

            public readonly bool IsBranchAttrFilterEnabled;

            internal readonly StatusProperty StatusAttribute;

            internal readonly bool IsAutoLabelEnabled;
            internal readonly string AutomaticLabelPattern;

            public PlasticSCM (
                bool bApprovedCodeReviewFilterEnabled,
                StatusProperty statusAttribute, 
                bool bAutoLabelEnabled, 
                string autoLabelPattern)
            {
                IsApprovedCodeReviewFilterEnabled = bApprovedCodeReviewFilterEnabled;

                StatusAttribute = statusAttribute;

                IsAutoLabelEnabled = bAutoLabelEnabled;
                AutomaticLabelPattern = autoLabelPattern;

                IsBranchAttrFilterEnabled =
                    !string.IsNullOrWhiteSpace(statusAttribute.Name) &&
                    !string.IsNullOrWhiteSpace(statusAttribute.ResolvedValue);
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
            internal readonly string PlanBranch;
            internal readonly string PlanAfterCheckin;
            internal readonly string[] BranchAttributeNamesToForwardBeforeCheckin;
            internal readonly string[] BranchAttributeNamesToForwardAfterCheckin;

            internal ContinuousIntegration(
                string plug, 
                string planBranch, 
                string planAfterCheckin,
                string[] branchAttributeNamesToForwardBeforeCheckin,
                string[] branchAttributeNamesToForwardAfterCheckin)
            {
                Plug = plug;
                PlanBranch = planBranch;
                PlanAfterCheckin = planAfterCheckin;
                BranchAttributeNamesToForwardBeforeCheckin = branchAttributeNamesToForwardBeforeCheckin;
                BranchAttributeNamesToForwardAfterCheckin = branchAttributeNamesToForwardAfterCheckin;
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

        public class StatusProperty
        {
            internal readonly string Name;
            internal readonly string ResolvedValue;
            internal readonly string TestingValue;
            internal readonly string FailedValue;
            internal readonly string MergedValue;

            public StatusProperty(
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

            internal string[] GetValues()
            {
                return new string[]
                {
                    ResolvedValue,
                    TestingValue,
                    FailedValue,
                    MergedValue
                };
            }
        }

        internal readonly string Server;
        internal readonly string Repository;
        internal readonly string TrunkBranch;
        internal readonly string BranchPrefix;
        internal readonly string UserApiKey;
        internal readonly bool QueueAgainOnFail;
        internal readonly PlasticSCM Plastic;
        internal readonly IssueTracker Issues;
        internal readonly ContinuousIntegration CI;
        internal readonly Notifier Notifications;

        internal static TrunkBotConfiguration BuildFromConfigFile(string configFile)
        {
            try
            {
                return BuildFromString(File.ReadAllText(configFile));
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat("Trunkbot configuration cannot be read from '{0}' : {1}",
                    configFile, ex.Message);
                mLog.DebugFormat("StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
                return null;
            }
        }

        internal static TrunkBotConfiguration BuildFromString(string configString)
        {
            JObject jsonObject = JObject.Parse(configString);

            if (jsonObject == null)
                return null;

            return new TrunkBotConfiguration(
                GetPropertyValue(jsonObject, "server"),
                GetPropertyValue(jsonObject, "repository"),
                GetPropertyValue(jsonObject, "trunk_branch"),
                GetPropertyValue(jsonObject, "branch_prefix"),
                GetPropertyValue(jsonObject, "bot_user"),
                GetBoolValue(jsonObject, "queue_again_on_fail", false),
                BuildPlasticSCM(jsonObject["plastic_group"]),
                BuildIssueTracker(jsonObject["issues_group"]),
                BuildContinuousIntegration(jsonObject["ci_group"]),
                BuildNotifier(jsonObject["notifier_group"]));
        }

        static PlasticSCM BuildPlasticSCM(JToken jsonToken)
        {
            if (jsonToken == null)
                return null;

            string automaticLabelPattern =
                jsonToken["label_group"] == null ?
                    string.Empty :
                    GetPropertyValue(jsonToken["label_group"], "pattern");

            return new PlasticSCM(
                GetBoolValue(jsonToken["code_review_group"], "is_enabled", false),
                BuildStatusProperty(jsonToken["status_attribute_group"]),
                GetBoolValue(jsonToken["label_group"], "is_enabled", false),
                automaticLabelPattern);
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

            string plug = GetPropertyValue(jsonToken, "plug");

            if (string.IsNullOrEmpty(plug))
                return null;

            return new ContinuousIntegration(
                plug,
                GetPropertyValue(jsonToken, "plan"),
                GetPropertyValue(jsonToken, "planAfterCheckin"),
                GetCommaOrSemicolonSeparatedValues(
                    GetPropertyValue(jsonToken, "branchAttributesToForward")),
                GetCommaOrSemicolonSeparatedValues(
                    GetPropertyValue(jsonToken, "branchAttributesToForwardAfterCheckin")));
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
                GetCommaOrSemicolonSeparatedValues(GetPropertyValue(jsonToken, "fixed_recipients")));
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

        static string[] GetCommaOrSemicolonSeparatedValues(string field)
        {
            if (field == null)
                return null;

            string[] result = field.Split(
                new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < result.Length; i++)
                result[i] = result[i].Trim();

            return result;
        }

        static string GetPropertyValue(JToken jsonToken, string key)
        {
            if (jsonToken == null)
                return null;

            JToken jsonProperty = jsonToken[key];

            if (jsonProperty == null)
                return null;

            return jsonProperty.Value<string>();
        }

        static bool GetBoolValue(JToken jsonToken, string key, bool defaultValue)
        {
            if (jsonToken == null)
                return defaultValue;

            JToken jsonProperty = jsonToken[key];

            if (jsonProperty == null)
                return defaultValue;

            bool fieldValue = false;

            if (jsonProperty.Type == JTokenType.Boolean)
            {
                fieldValue = jsonProperty.Value<bool>();
                return fieldValue;
            }

            if (jsonProperty.Type != JTokenType.String)
                throw new NotSupportedException(
                    string.Format("Value {0} is not supported", jsonProperty.ToString()));

            string valueStr = jsonProperty.Value<string>();
            if ("yes".Equals(valueStr, StringComparison.OrdinalIgnoreCase))
            {
                fieldValue = true;
                return fieldValue;
            }

            if ("no".Equals(valueStr, StringComparison.OrdinalIgnoreCase))
            {
                fieldValue = false;
                return false;
            }

            throw new NotSupportedException(
                string.Format("Value {0} is not supported", valueStr));
        }

        TrunkBotConfiguration(
            string server,
            string repository,
            string trunkBranch,
            string branchPrefix,
            string userApiKey,
            bool queueAgainOnFail,
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
            QueueAgainOnFail = queueAgainOnFail;
            Plastic = plastic;
            Issues = issues;
            CI = ci;
            Notifications = notifications;
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-TrunkBotConfiguration");
    }
}
