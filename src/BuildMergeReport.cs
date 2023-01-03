using System;
using System.Collections.Generic;

using Codice.CM.Server.Devops;

namespace TrunkBot
{
    internal static class BuildMergeReport
    {
        internal static MergeReport Build(string repId, int branchId)
        {
            MergeReport result = new MergeReport();
            result.Timestamp = DateTime.UtcNow;
            result.RepositoryId = repId;
            result.BranchId = branchId;
            result.Properties = new List<MergeReport.Entry>();
            return result;
        }

        internal static void AddIssueProperty(
            MergeReport mergeReport,
            string issueTitle, string issueLink)
        {
            MergeReport.Entry issueProperty = new MergeReport.Entry();
            issueProperty.Text = issueTitle;
            issueProperty.Link = issueLink;
            issueProperty.Type = ISSUETRACKER_TYPE;

            mergeReport.Properties.Add(issueProperty);
        }

        internal static void AddFailedMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status, string message)
        {
            MergeReport.Entry failedMergeProperty = new MergeReport.Entry();
            failedMergeProperty.Text = GetMergeToResultStatus(status);
            failedMergeProperty.Type = MERGE_FAILED_TYPE;
            failedMergeProperty.Value = message;

            mergeReport.Properties.Add(failedMergeProperty);
        }

        internal static void AddSucceededMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status)
        {
            MergeReport.Entry succeededMergeProperty = new MergeReport.Entry();
            succeededMergeProperty.Text = GetMergeToResultStatus(status);
            succeededMergeProperty.Type = MERGE_OK_TYPE;

            mergeReport.Properties.Add(succeededMergeProperty);
        }

        internal static void UpdateMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status, int csetId)
        {
            MergeReport.Entry mergeProperty = FindPropertyByType(
                mergeReport.Properties, MERGE_OK_TYPE);

            if (mergeProperty == null)
                return;

            if (csetId == -1)
            {
                mergeProperty.Text = GetMergeToResultStatus(status);
                mergeProperty.Type = MERGE_FAILED_TYPE;
                return;
            }

            mergeProperty.Value = csetId.ToString();
        }

        internal static void AddLabelProperty(
            MergeReport mergeReport,
            bool isSuccessfulOperation,
            string labelName,
            string message)
        {
            MergeReport.Entry labelActionProperty = new MergeReport.Entry();
            labelActionProperty.Text = string.Format(
                "label {0} ({1})", isSuccessfulOperation ? "ok" : "ko", labelName);
            labelActionProperty.Value = message;
            labelActionProperty.Type = isSuccessfulOperation ?
                LABEL_OK_TYPE :
                LABEL_FAILED_TYPE;

            mergeReport.Properties.Add(labelActionProperty);
        }

        internal static void AddFailedBuildProperty(
            MergeReport mergeReport, string planBranch, string message)
        {
            MergeReport.Entry failedBuildProperty = new MergeReport.Entry();
            failedBuildProperty.Text = string.Format(
                "build ko (plan: {0})", planBranch);
            failedBuildProperty.Type = BUILD_FAILED_TYPE;
            failedBuildProperty.Value = message;

            mergeReport.Properties.Add(failedBuildProperty);
        }

        internal static void AddSucceededBuildProperty(
            MergeReport mergeReport, string planBranch)
        {
            MergeReport.Entry succeededBuildProperty = new MergeReport.Entry();
            succeededBuildProperty.Text = string.Format(
                "build ok (plan: {0})", planBranch);
            succeededBuildProperty.Type = BUILD_OK_TYPE;

            mergeReport.Properties.Add(succeededBuildProperty);
        }

        internal static void SetUnexpectedExceptionProperty(
            MergeReport mergeReport, string message)
        {
            if (mergeReport == null)
                return;

            MergeReport.Entry failedBuildProperty = FindPropertyByType(
                mergeReport.Properties, BUILD_FAILED_TYPE);

            if (failedBuildProperty == null)
            {
                failedBuildProperty = new MergeReport.Entry();
                failedBuildProperty.Type = BUILD_FAILED_TYPE;
                mergeReport.Properties.Add(failedBuildProperty);
            }

            failedBuildProperty.Text = "build ko (unexpected exception)";
            failedBuildProperty.Value = message;
        }

        internal static void AddBuildTimeProperty(
            MergeReport mergeReport, int timeInMilliseconds)
        {
            AddNumberProperty(
                mergeReport, "build time (min)",
                GetMinutes(timeInMilliseconds));
        }

        static void AddNumberProperty(
            MergeReport mergeReport, string text, double value)
        {
            MergeReport.Entry numberProperty = new MergeReport.Entry();
            numberProperty.Text = text;
            numberProperty.Type = "number";
            numberProperty.Value = value.ToString();

            mergeReport.Properties.Add(numberProperty);
        }

        static MergeReport.Entry FindPropertyByType(
            List<MergeReport.Entry> properties, string type)
        {
            foreach (MergeReport.Entry property in properties)
            {
                if (property.Type == type)
                    return property;
            }

            return null;
        }

        static string GetMergeToResultStatus(MergeToResultStatus status)
        {
            switch (status)
            {
                case MergeToResultStatus.OK:
                    return "ok";
                case MergeToResultStatus.AncestorNotFound:
                    return "ancestor_not_found";
                case MergeToResultStatus.MergeNotNeeded:
                    return "merge_not_needed";
                case MergeToResultStatus.Conflicts:
                    return "conflicts";
                case MergeToResultStatus.DestinationChanges:
                    return "destination_changes";
                case MergeToResultStatus.Error:
                    return "error";
                case MergeToResultStatus.MultipleHeads:
                    return "multiple_heads";
                default:
                    return string.Empty;
            }
        }

        static double GetMinutes(int milliseconds)
        {
            double totalMinutes = TimeSpan.FromMilliseconds(milliseconds).TotalMinutes;
            return Math.Round(totalMinutes, 2);
        }

        const string BUILD_FAILED_TYPE = "build_failed";
        const string BUILD_OK_TYPE = "build_ok";
        const string MERGE_FAILED_TYPE = "merge_failed";
        const string MERGE_OK_TYPE = "merge_ok";
        const string LABEL_FAILED_TYPE = "label_failed";
        const string LABEL_OK_TYPE = "label_ok";
        const string ISSUETRACKER_TYPE = "issuetracker";
    }
}
