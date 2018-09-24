using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;
using TrunkBot.Messages;

namespace TrunkBot
{
    internal class TrunkMergebotApi
    {
        internal static class Users
        {
            internal static JObject GetUserProfile(RestApi restApi, string userName)
            {
                return restApi.Users.GetUserProfile(userName);
            }
        }

        internal static class MergeReports
        {
            internal static void ReportMerge(RestApi restApi, string mergebotName, MergeReport mergeReport)
            {
                restApi.MergeReports.ReportMerge(mergebotName, mergeReport);
            }
        }

        internal class Issues
        {
            internal static bool Connected(
                RestApi restApi,
                string issueTrackerName)
            {
                SingleResponse response = restApi.Issues.IsConnected(issueTrackerName);
                return GetBoolValue(response.Value, false);
            }

            internal static void SetIssueField(
                RestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber, string fieldName, string fieldValue)
            {
                SetIssueFieldRequest request = new SetIssueFieldRequest()
                {
                    NewValue = fieldValue
                };

                restApi.Issues.SetIssueField(issueTrackerName,
                    projectKey, taskNumber, fieldName, request);
            }

            internal static string GetIssueUrl(
                RestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber)
            {
                return restApi.Issues.GetIssueUrl(issueTrackerName,
                    projectKey, taskNumber).Value;
            }

            internal static string GetIssueField(
                RestApi restApi,
                string issueTrackerName,
                string projectKey,
                string taskNumber, string fieldName)
            {
                return restApi.Issues.GetIssueField(issueTrackerName,
                    projectKey, taskNumber, fieldName).Value;
            }
        }

        internal class Notify
        {
            internal static void Message(
                RestApi restApi,
                string notifierName, string message, List<string> recipients)
            {
                NotifyMessageRequest request = new NotifyMessageRequest()
                {
                    Message = message,
                    Recipients = recipients
                };

                restApi.Notify.NotifyMessage(notifierName, request);
            }
        }

        internal class CI
        {
            internal class PlanResult
            {
                internal bool Succeeded;
                internal string Explanation;
            }

            internal static PlanResult Build(
                RestApi restApi,
                string ciName,
                string planBranch,
                string scmSpecToSwitchTo,
                string comment,
                BuildProperties properties)
            {
                return Run(
                    restApi,
                    ciName,
                    planBranch,
                    scmSpecToSwitchTo,
                    comment,
                    ParseBuildProperties.ToDictionary(properties),
                    maxWaitTimeSeconds: 4 * 60 * 60);
            }

            static PlanResult Run(
                RestApi restApi,
                string ciName,
                string planName,
                string objectSpec,
                string comment,
                Dictionary<string, string> properties,
                int maxWaitTimeSeconds)
            {
                LaunchPlanRequest request = new LaunchPlanRequest()
                {
                    ObjectSpec = objectSpec,
                    Comment = string.Format("MergeBot - {0}", comment),
                    Properties = properties
                };

                SingleResponse planResponse = restApi.CI.LaunchPlan(
                    ciName, planName, request);

                GetPlanStatusResponse statusResponse =
                    Task.Run(() =>
                        WaitForFinishedPlanStatus(
                            restApi,
                            ciName, planName, planResponse.Value,
                            maxWaitTimeSeconds)
                        ).Result;

                if (statusResponse != null)
                {
                    return new PlanResult()
                    {
                        Succeeded = statusResponse.Succeeded,
                        Explanation = statusResponse.Explanation
                    };
                }

                return new PlanResult()
                {
                    Succeeded = false,
                    Explanation = string.Format(
                        "{0} reached the time limit to get the status " +
                        "for plan:'{1}' and executionId:'{2}'" +
                        "\nRequest details: objectSpec:'{3}' and comment:'{4}'",
                        ciName, planName, planResponse.Value, objectSpec, comment)
                };
            }

            static async Task<GetPlanStatusResponse> WaitForFinishedPlanStatus(
                RestApi restApi,
                string ciName,
                string planBranch,
                string executionId,
                int maxWaitTimeSeconds)
            {
                long startTime = Environment.TickCount;
                do
                {
                    GetPlanStatusResponse statusResponse = restApi.CI.
                        GetPlanStatus(ciName, planBranch, executionId);

                    if (statusResponse.IsFinished)
                        return statusResponse;

                    await Task.Delay(5000);

                } while (Environment.TickCount - startTime < maxWaitTimeSeconds * 1000);

                return null;
            }
        }

        internal static BranchModel GetBranch(
            RestApi restApi,
            string repoName, string branchName)
        {
            return restApi.GetBranch(repoName, branchName);
        }

        internal static int GetBranchHead(
            RestApi restApi,
            string repoName, string branchName)
        {
            return restApi.GetBranch(repoName, branchName).HeadChangeset;
        }

        internal static ChangesetModel GetChangeset(
            RestApi restApi, string repoName, int changesetId)
        {
            return restApi.GetChangeset(repoName, changesetId);
        }

        internal static string GetBranchAttribute(
            RestApi restApi,
            string repoName, string branchName, string attributeName)
        {
            return restApi.GetAttribute(repoName, attributeName,
                AttributeTargetType.Branch, branchName).Value;
        }

        internal static void ChangeBranchAttribute(
            RestApi restApi,
            string repoName, string branchName, string attributeName, string attributeValue)
        {
            ChangeAttributeRequest request = new ChangeAttributeRequest()
            {
                TargetType = AttributeTargetType.Branch,
                TargetName = branchName,
                Value = attributeValue
            };

            restApi.ChangeAttribute(repoName, attributeName, request);
        }

        [Flags]
        internal enum MergeToOptions : byte
        {
            None = 0,
            CreateShelve = 1 << 0,
            EnsureNoDstChanges = 1 << 1,
        }

        internal static MergeToResponse MergeBranchTo(
            RestApi restApi,
            string repoName,
            string sourceBranch,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return MergeTo(
                restApi, repoName, sourceBranch, MergeToSourceType.Branch,
                destinationBranch, comment, options);
        }

        internal static MergeToResponse MergeShelveTo(
            RestApi restApi,
            string repoName,
            int shelveId,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return MergeTo(
                restApi, repoName, shelveId.ToString(), MergeToSourceType.Shelve,
                destinationBranch, comment, options);
        }

        internal static JArray Find(
            RestApi restApi,
            string repName,
            string query,
            string queryDateFormat,
            string[] fields)
        {
            return restApi.Find(repName, query, queryDateFormat, fields);
        }

        internal static void DeleteShelve(
            RestApi restApi,
            string repoName, int shelveId)
        {
            restApi.DeleteShelve(repoName, shelveId);
        }

        static MergeToResponse MergeTo(
            RestApi restApi,
            string repoName,
            string source,
            MergeToSourceType sourceType,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            MergeToRequest request = new MergeToRequest()
            {
                SourceType = sourceType,
                Source = source,
                Destination = destinationBranch,
                Comment = comment,
                CreateShelve = options.HasFlag(MergeToOptions.CreateShelve),
                EnsureNoDstChanges = options.HasFlag(MergeToOptions.EnsureNoDstChanges)
            };

            return restApi.MergeTo(repoName, request);
        }

        static bool GetBoolValue(string value, bool defaultValue)
        {
            bool flag;
            if (Boolean.TryParse(value, out flag))
                return flag;

            return defaultValue;
        }
    }
}
