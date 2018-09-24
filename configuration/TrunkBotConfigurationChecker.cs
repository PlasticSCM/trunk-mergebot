namespace TrunkBot.Configuration
{
    internal class TrunkBotConfigurationChecker
    {
        internal static bool CheckConfiguration(
            TrunkBotConfiguration botConfig,
            out string errorMessage)
        {
            if (!CheckValidFields(botConfig, out errorMessage))
            {
                errorMessage = string.Format(
                    "trunkbot can't start without specifying a valid config for the following fields:\n{0}",
                    errorMessage);
                return false;
            }

            return true;
        }

        internal static bool CheckValidFields(
            TrunkBotConfiguration botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Server))
                errorMessage += BuildFieldError("server");

            if (string.IsNullOrEmpty(botConfig.Repository))
                errorMessage += BuildFieldError("repository");

            if (string.IsNullOrEmpty(botConfig.TrunkBranch))
                errorMessage += BuildFieldError("trunk branch");

            if (string.IsNullOrEmpty(botConfig.UserApiKey))
                errorMessage += BuildFieldError("user api key");

            string propertyErrorMessage = null;
            if (!CheckValidPlasticFields(botConfig.Plastic, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            propertyErrorMessage = null;
            if (!CheckValidIssueTrackerFields(botConfig.Issues, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            propertyErrorMessage = null;
            if (!CheckValidContinuousIntegrationFields(botConfig.CI, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            propertyErrorMessage = null;
            if (!CheckValidNotifierFields(botConfig.Notifications, out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidPlasticFields(
            TrunkBotConfiguration.PlasticSCM botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
            {
                errorMessage = BuildFieldError("Plastic SCM advanced configuration");
                return false;
            }

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFields(
                    botConfig.StatusAttribute,
                    "of the status attribute for Plastic config",
                    out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidIssueTrackerFields(
            TrunkBotConfiguration.IssueTracker botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildFieldError("plug name for Issue Tracker config");

            if (string.IsNullOrEmpty(botConfig.TitleField))
                errorMessage += BuildFieldError("title field for Issue Tracker config");

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFields(
                    botConfig.StatusField,
                    "of the status field for Issue Tracker config",
                    out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidContinuousIntegrationFields(
            TrunkBotConfiguration.ContinuousIntegration botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
            {
                errorMessage = BuildFieldError("CI Integration configuration");
                return false;
            }

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildFieldError("plug name for CI config");

            if (string.IsNullOrEmpty(botConfig.Plan))
                errorMessage += BuildFieldError("plan for CI config");

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidNotifierFields(
            TrunkBotConfiguration.Notifier botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildFieldError("plug name for Notifications config");

            if (IsDestinationInfoEmpty(botConfig))
            {
                errorMessage += "* There is no destination info in the Notifications" +
                    " config. Please specify a user profile field, a list of recipients" +
                    " or both (recommended).\n";
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidStatusPropertyFields(
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Name))
                errorMessage += BuildFieldError("name " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.ResolvedValue))
                errorMessage += BuildFieldError("resolved value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.FailedValue))
                errorMessage += BuildFieldError("failed value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.MergedValue))
                errorMessage += BuildFieldError("merged value " + groupNameMessage);

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool IsDestinationInfoEmpty(TrunkBotConfiguration.Notifier botConfig)
        {
            return string.IsNullOrEmpty(botConfig.UserProfileField) &&
                (botConfig.FixedRecipients == null || botConfig.FixedRecipients.Length == 0);
        }

        static string BuildFieldError(string fieldName)
        {
            return string.Format("* The {0} must be defined.\n", fieldName);
        }
    }
}
