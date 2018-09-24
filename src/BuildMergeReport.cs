using System;
using System.Collections.Generic;

using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;

namespace TrunkBot
{
    internal static class BuildMergeReport
    {
        internal static MergeReport Build(BranchModel branch)
        {
            MergeReport result = new MergeReport();
            result.Timestamp = DateTime.UtcNow;
            result.RepositoryId = branch.RepositoryId;
            result.BranchId = branch.Id;
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
            issueProperty.Type = "issuetracker";

            mergeReport.Properties.Add(issueProperty);
        }

        internal static void AddFailedMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status, string message)
        {
            MergeReport.Entry failedMergeProperty = new MergeReport.Entry();
            failedMergeProperty.Text = GetMergeToResultStatus(status);
            failedMergeProperty.Type = "merge_failed";
            failedMergeProperty.Value = message;

            mergeReport.Properties.Add(failedMergeProperty);
        }

        internal static void AddSucceededMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status)
        {
            MergeReport.Entry succeededMergeProperty = new MergeReport.Entry();
            succeededMergeProperty.Text = GetMergeToResultStatus(status);
            succeededMergeProperty.Type = "merge_ok";

            mergeReport.Properties.Add(succeededMergeProperty);
        }

        internal static void UpdateMergeProperty(
            MergeReport mergeReport, MergeToResultStatus status, int csetId)
        {
            MergeReport.Entry mergeProperty = FindPropertyByType(
                mergeReport.Properties, "merge_ok");

            if (mergeProperty == null)
                return;

            if (csetId == -1)
            {
                mergeProperty.Text = GetMergeToResultStatus(status);
                mergeProperty.Type = "merge_failed";
                return;
            }

            mergeProperty.Value = csetId.ToString();
        }

        internal static void AddFailedBuildProperty(
            MergeReport mergeReport, string planBranch, string message)
        {
            MergeReport.Entry failedBuildProperty = new MergeReport.Entry();
            failedBuildProperty.Text = string.Format(
                "build ko (plan: {0})", planBranch);
            failedBuildProperty.Type = "build_failed";
            failedBuildProperty.Value = message;

            mergeReport.Properties.Add(failedBuildProperty);
        }

        internal static void AddSucceededBuildProperty(
            MergeReport mergeReport, string planBranch)
        {
            MergeReport.Entry succeededBuildProperty = new MergeReport.Entry();
            succeededBuildProperty.Text = string.Format(
                "build ok (plan: {0})", planBranch);
            succeededBuildProperty.Type = "build_ok";

            mergeReport.Properties.Add(succeededBuildProperty);
        }

        internal static void SetUnexpectedExceptionProperty(
            MergeReport mergeReport, string message)
        {
            if (mergeReport == null)
                return;

            MergeReport.Entry failedBuildProperty = FindPropertyByType(
                mergeReport.Properties, "build_failed");

            if (failedBuildProperty == null)
            {
                failedBuildProperty = new MergeReport.Entry();
                failedBuildProperty.Type = "build_failed";
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
    }
}
