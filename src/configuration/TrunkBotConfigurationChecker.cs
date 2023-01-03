using System;
namespace TrunkBot.Configuration
{
    public class TrunkBotConfigurationChecker
    {
        public static bool CheckConfiguration(
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

        public static bool CheckValidFields(
            TrunkBotConfiguration botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Server))
                errorMessage += BuildMissingFieldError("server");

            if (string.IsNullOrEmpty(botConfig.Repository))
                errorMessage += BuildMissingFieldError("repository");

            if (string.IsNullOrEmpty(botConfig.TrunkBranch))
                errorMessage += BuildMissingFieldError("trunk branch");

            if (string.IsNullOrEmpty(botConfig.UserApiKey))
                errorMessage += BuildMissingFieldError("user api key");

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

        public static bool CheckValidPlasticFields(
            TrunkBotConfiguration.PlasticSCM botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            string fieldNameBeingChecked = "Branch lifecycle and automatic labels";

            if (botConfig == null)
            {
                errorMessage = BuildMissingFieldError(fieldNameBeingChecked);
                return false;
            }

            if (!AreAnyFiltersDefined(botConfig))
            {
                errorMessage = BuildNoFiltersEnabledErrorMessage(fieldNameBeingChecked);
                return false;
            }

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFieldsForPlasticAttr(
                    botConfig.IsApprovedCodeReviewFilterEnabled,
                    botConfig.StatusAttribute,
                    "of the 'Branch lifecycle configuration with a status attribute'",
                    out propertyErrorMessage))
                errorMessage += propertyErrorMessage;

            string labelPropertiesErrorMessage = null;
            if (!CheckValidAutomaticLabelFields(
                    botConfig.IsAutoLabelEnabled, botConfig.AutomaticLabelPattern,
                    out labelPropertiesErrorMessage))
                errorMessage += labelPropertiesErrorMessage;

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool AreAnyFiltersDefined(TrunkBotConfiguration.PlasticSCM botConfig)
        {
            if (botConfig.IsApprovedCodeReviewFilterEnabled)
                return true;

            if (string.IsNullOrWhiteSpace(botConfig.StatusAttribute.Name))
                return false;

            if (string.IsNullOrWhiteSpace(botConfig.StatusAttribute.ResolvedValue))
                return false;

            return true;
        }

        static bool CheckValidIssueTrackerFields(
            TrunkBotConfiguration.IssueTracker botConfig,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (botConfig == null)
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildMissingFieldError("plug name for Issue Tracker config");

            if (string.IsNullOrEmpty(botConfig.TitleField))
                errorMessage += BuildMissingFieldError("title field for Issue Tracker config");

            string propertyErrorMessage = null;
            if (!CheckValidStatusPropertyFieldsForIssueTracker(
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
                return true;

            if (string.IsNullOrEmpty(botConfig.Plug))
                errorMessage += BuildMissingFieldError("plug name for CI config");

            if (string.IsNullOrEmpty(botConfig.PlanBranch))
                errorMessage += BuildMissingFieldError("plan branch for CI config");

            //botConfig.PlanAfterCheckin could be empty, so we don't check its field.

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
                errorMessage += BuildMissingFieldError("plug name for Notifications config");

            if (IsDestinationInfoEmpty(botConfig))
            {
                errorMessage += "* There is no destination info in the Notifications" +
                    " config. Please specify a user profile field, a list of recipients" +
                    " or both (recommended).\n";
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidStatusPropertyFieldsForIssueTracker(
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            return CheckValidStatusPropertyFieldsForPlasticAttr(
                false, botConfig, groupNameMessage, out errorMessage);
        }

        static bool CheckValidStatusPropertyFieldsForPlasticAttr(
            bool bIsApprovedCodeReviewFilterEnabled,
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            return CheckValidStatusPropertyFields(
                bIsApprovedCodeReviewFilterEnabled, botConfig, groupNameMessage, out errorMessage);
        }

        static bool CheckValidStatusPropertyFields(
            bool bIsApprovedCodeReviewFilterEnabled,
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage,
            out string errorMessage)
        {
            errorMessage = CheckMissingStatusFields(
                bIsApprovedCodeReviewFilterEnabled, botConfig, groupNameMessage);

            errorMessage += CheckInvalidCharactersInStatusFields(botConfig, groupNameMessage);

            if (!string.IsNullOrEmpty(botConfig.ResolvedValue) &&
                !string.IsNullOrEmpty(botConfig.MergedValue) &&
                botConfig.ResolvedValue.Equals(
                    botConfig.MergedValue, StringComparison.InvariantCultureIgnoreCase))
            {
                errorMessage += string.Format(
                    "The 'merged' attribute value: [{0}] must " +
                    "be different than 'resolved' attribute value: [{1}] (case insensitive)\n",
                    botConfig.ResolvedValue, botConfig.MergedValue);
            }

            if (!string.IsNullOrEmpty(botConfig.ResolvedValue) &&
                !string.IsNullOrEmpty(botConfig.FailedValue) &&
                botConfig.ResolvedValue.Equals(
                    botConfig.FailedValue, StringComparison.InvariantCultureIgnoreCase))
            {
                errorMessage += string.Format(
                    "The 'failed' attribute value: [{0}] must " +
                    "be different than 'resolved' attribute value: [{1}] (case insensitive)\n",
                    botConfig.ResolvedValue, botConfig.FailedValue);
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool CheckValidAutomaticLabelFields(
            bool isAutoLabelEnabled, 
            string automaticLabelPattern, 
            out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!isAutoLabelEnabled)
                return true;

            if (string.IsNullOrEmpty(automaticLabelPattern))
            {
                errorMessage += "The label pattern specification field cannot be empty";
                return false;
            }

            if (string.IsNullOrEmpty(
                Labeling.AutomaticLabeler.PatternTranslator.ToFindPattern(
                    automaticLabelPattern, System.DateTime.Now)))
            {
                errorMessage += 
                    "The label pattern specification cannot be parsed. " +
                    "Check if you have invalid variable specifications " +
                    "(unclosed variables) or nested variables (not allowed).";
                return false;
            }

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool IsDestinationInfoEmpty(TrunkBotConfiguration.Notifier botConfig)
        {
            return string.IsNullOrEmpty(botConfig.UserProfileField) &&
                (botConfig.FixedRecipients == null || botConfig.FixedRecipients.Length == 0);
        }

        static string CheckMissingStatusFields(
            bool bIsApprovedCodeReviewFilterEnabled,
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage)
        {
            string errorMessage = string.Empty;

            if (string.IsNullOrEmpty(botConfig.Name))
                errorMessage += BuildMissingFieldError("name " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.ResolvedValue) && !bIsApprovedCodeReviewFilterEnabled)
                errorMessage += BuildMissingFieldError("resolved value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.FailedValue) && !bIsApprovedCodeReviewFilterEnabled)
                errorMessage += BuildMissingFieldError("failed value " + groupNameMessage);

            if (string.IsNullOrEmpty(botConfig.MergedValue))
                errorMessage += BuildMissingFieldError("merged value " + groupNameMessage);

            return errorMessage;
        }

        static string CheckInvalidCharactersInStatusFields(
            TrunkBotConfiguration.StatusProperty botConfig,
            string groupNameMessage)
        {
            string errorMessage = string.Empty;

            if (ContainsInvalidCharacters(botConfig.Name))
            {
                errorMessage += BuildInvalidCharactersInFieldError(
                    "status attribute name " + groupNameMessage);
            }

            if (ContainsInvalidCharacters(botConfig.ResolvedValue))
            {
                errorMessage += BuildInvalidCharactersInFieldError(
                    "resolved value " + groupNameMessage);
            }

            if (ContainsInvalidCharacters(botConfig.TestingValue))
            {
                errorMessage += BuildInvalidCharactersInFieldError(
                    "testing value " + groupNameMessage);
            }

            if (ContainsInvalidCharacters(botConfig.FailedValue))
            {
                errorMessage += BuildInvalidCharactersInFieldError(
                    "failed value " + groupNameMessage);
            }

            if (ContainsInvalidCharacters(botConfig.MergedValue))
            {
                errorMessage += BuildInvalidCharactersInFieldError(
                    "merged value " + groupNameMessage);
            }
            return errorMessage;
        }

        static string BuildMissingFieldError(string fieldName)
        {
            return string.Format("* The {0} must be defined.\n", fieldName);
        }

        static bool ContainsInvalidCharacters(string value)
        {
            return value.Contains("\"") || value.Contains("'");
        }

        static string BuildInvalidCharactersInFieldError(string fieldName)
        {
            return string.Format(
                "* At least one unsupported character [\", '] found in the {0}.\n",
                fieldName);
        }

        static string BuildNoFiltersEnabledErrorMessage(string fieldName)
        {
            return string.Format(
                "* Either the 'Process reviewed branches only' or the 'Branch lifecycle configuration " +
                "with a status attribute' must be properly enabled in the '{0}' section.", fieldName);
        }
    }
}
