using System;

using log4net;

using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;
using TrunkBot.Configuration;
using TrunkBot.Messages;

namespace TrunkBot
{
    internal static class ProcessBranch
    {
        internal enum Result
        {
            NotReady,
            Ok,
            Failed
        };

        internal static Result TryProcessBranch(
            RestApi restApi,
            Branch branch,
            TrunkBotConfiguration botConfig,
            string botName)
        {
            int shelveId = -1;

            string taskNumber = null;
            MergeReport mergeReport = null;
            try
            {
                mLog.InfoFormat("Getting task number of branch {0} ...", branch.FullName);
                taskNumber = GetTaskNumber(branch.FullName, botConfig.BranchPrefix);
                if (!IsTaskReady(restApi, taskNumber, botConfig.Issues))
                    return Result.NotReady;

                mLog.InfoFormat("Building the merge report of task {0} ...", taskNumber);
                mergeReport = BuildMergeReport.Build(TrunkMergebotApi.GetBranch(
                    restApi, branch.Repository, branch.FullName));

                string taskTittle;
                string taskUrl;
                if (GetIssueInfo(restApi, taskNumber, botConfig.Issues,
                        out taskTittle, out taskUrl))
                    BuildMergeReport.AddIssueProperty(mergeReport, taskTittle, taskUrl);

                string comment = GetComment(branch.FullName, taskTittle, botName);

                mLog.InfoFormat("Trying to shelve server-side-merge from {0} to {1}",
                    branch.FullName, botConfig.TrunkBranch);

                if (!MergeToOperations.TryMergeToShelve(
                        restApi, branch, botConfig.TrunkBranch, mergeReport,
                        comment, taskNumber, botConfig,
                        out shelveId))
                    return Result.Failed;

                mLog.InfoFormat("Testing branch {0} ...", branch.FullName);
                if (!TryBuildTask(restApi, branch, mergeReport,
                        taskNumber, shelveId, botConfig))
                    return Result.Failed;

                mLog.InfoFormat("Checking-in shelved merged {0} from {1} to {2}",
                    shelveId, branch.FullName, botConfig.TrunkBranch);

                int csetId = -1;
                if (!MergeToOperations.TryApplyShelve(
                        restApi, branch, botConfig.TrunkBranch, mergeReport,
                        comment, taskNumber, shelveId, botConfig,
                        out csetId))
                    return Result.Failed;

                mLog.InfoFormat("Checkin: Created changeset {0} in branch {1}",
                    csetId, botConfig.TrunkBranch);

                mLog.InfoFormat("Setting branch {0} as 'integrated'...", branch.FullName);
                ChangeTaskStatus.SetTaskAsMerged(
                    restApi,
                    branch,
                    taskNumber,
                    string.Format(
                        "Branch {0} was correctly merged to {1}.",
                        branch.FullName,
                        botConfig.TrunkBranch),
                    botConfig);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "Try process task {0} failed for branch {1}: {2}",
                    taskNumber, branch.FullName, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}", Environment.NewLine, ex.StackTrace);

                ChangeTaskStatus.SetTaskAsFailed(
                    restApi,
                    branch,
                    taskNumber,
                    string.Format(
                        "Can't process branch {0} because of an unexpected error: {1}.",
                        branch.FullName,
                        ex.Message),
                    botConfig);

                BuildMergeReport.SetUnexpectedExceptionProperty(mergeReport, ex.Message);

                return Result.Failed;
            }
            finally
            {
                ReportMerge(restApi, branch.Repository, branch.FullName, botName, mergeReport);

                SafeDeleteShelve(restApi, branch.Repository, shelveId);
            }

            return Result.Ok;
        }

        static bool TryBuildTask(
            RestApi restApi,
            Branch branch,
            MergeReport mergeReport,
            string taskNumber,
            int shelveId,
            TrunkBotConfiguration botConfig)
        {
            ChangeTaskStatus.SetTaskAsTesting(restApi, branch, taskNumber, string.Format(
                "Starting to test branch {0}.", branch.FullName), botConfig);

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Format("sh:{0}@{1}", shelveId, repSpec);

            string comment = string.Format(
                "Building branch {0}", branch.FullName);

            BuildProperties properties = CreateBuildProperties(
                restApi, taskNumber, branch.FullName, botConfig);

            int iniTime = Environment.TickCount;

            TrunkMergebotApi.CI.PlanResult buildResult = TrunkMergebotApi.CI.Build(
                restApi, botConfig.CI.Plug, botConfig.CI.Plan,
                scmSpecToSwitchTo, comment, properties);

            BuildMergeReport.AddBuildTimeProperty(mergeReport,
                Environment.TickCount - iniTime);

            if (buildResult.Succeeded)
            {
                BuildMergeReport.AddSucceededBuildProperty(mergeReport, botConfig.CI.Plan);

                return true;
            }

            BuildMergeReport.AddFailedBuildProperty(mergeReport,
                botConfig.CI.Plan, buildResult.Explanation);

            ChangeTaskStatus.SetTaskAsFailed(
                restApi,
                branch,
                taskNumber,
                string.Format(
                    "Branch {0} build failed. \nReason: {1}",
                    branch.FullName,
                    buildResult.Explanation),
                botConfig);

            return false;
        }

        static string GetTaskNumber(
            string branch,
            string branchPrefix)
        {
            string branchName = BranchSpec.GetName(branch);

            if (string.IsNullOrEmpty(branchPrefix))
                return branchName;

            if (branchName.StartsWith(branchPrefix,
                    StringComparison.InvariantCultureIgnoreCase))
                return branchName.Substring(branchPrefix.Length);

            return null;
        }

        static string GetComment(
            string branch,
            string taskTittle,
            string botName)
        {
            string comment = string.Format("{0}: merged {1}", botName, branch);

            if (taskTittle != null)
                comment += " : " + taskTittle;

            return comment;
        }

        static bool IsTaskReady(
            RestApi restApi,
            string taskNumber,
            TrunkBotConfiguration.IssueTracker issuesConfig)
        {
            if (taskNumber == null)
                return false;

            if (issuesConfig == null)
                return true;

            mLog.InfoFormat("Checking if issue tracker [{0}] is available...", issuesConfig.Plug);
            if (!TrunkMergebotApi.Issues.Connected(restApi, issuesConfig.Plug))
            {
                mLog.WarnFormat("Issue tracker [{0}] is NOT available...", issuesConfig.Plug);
                return false;
            }

            mLog.InfoFormat("Checking if task {0} is ready in the issue tracker [{1}].",
                taskNumber, issuesConfig.Plug);

            string status = TrunkMergebotApi.Issues.GetIssueField(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber, issuesConfig.StatusField.Name);

            mLog.DebugFormat("Issue tracker status for task [{0}]: expected [{1}], was [{2}]",
                taskNumber, issuesConfig.StatusField.ResolvedValue, status);

            return status == issuesConfig.StatusField.ResolvedValue;
        }

        static bool GetIssueInfo(
            RestApi restApi,
            string taskNumber,
            TrunkBotConfiguration.IssueTracker issuesConfig,
            out string taskTittle,
            out string taskUrl)
        {
            taskTittle = null;
            taskUrl = null;

            if (issuesConfig == null)
                return false;

            mLog.InfoFormat("Obtaining task {0} title...", taskNumber);
            taskTittle = TrunkMergebotApi.Issues.GetIssueField(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber, issuesConfig.TitleField);

            mLog.InfoFormat("Obtaining task {0} url...", taskNumber);
            taskUrl = TrunkMergebotApi.Issues.GetIssueUrl(
                restApi, issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber);

            return true;
        }

        static void ReportMerge(
            RestApi restApi,
            string repository,
            string branchName,
            string botName,
            MergeReport mergeReport)
        {
            if (mergeReport == null)
                return;

            try
            {
                TrunkMergebotApi.MergeReports.ReportMerge(restApi, botName, mergeReport);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "Unable to report merge for branch {0} on repository {1}: {2}",
                    branchName, repository, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }
        }

        static void SafeDeleteShelve(
            RestApi restApi,
            string repository,
            int shelveId)
        {
            if (shelveId == -1)
                return;

            try
            {
                TrunkMergebotApi.DeleteShelve(restApi, repository, shelveId);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "Unable to delete shelve {0} on repository {1}: {2}",
                    shelveId, repository, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }
        }

        static BuildProperties CreateBuildProperties(
            RestApi restApi,
            string taskNumber,
            string branchName,
            TrunkBotConfiguration botConfig)
        {
            int branchHeadChangesetId = TrunkMergebotApi.GetBranchHead(
                restApi, botConfig.Repository, branchName);
            ChangesetModel branchHeadChangeset = TrunkMergebotApi.GetChangeset(
                restApi, botConfig.Repository, branchHeadChangesetId);

            int trunkHeadChangesetId = TrunkMergebotApi.GetBranchHead(
                restApi, botConfig.Repository, botConfig.TrunkBranch);
            ChangesetModel trunkHeadChangeset = TrunkMergebotApi.GetChangeset(
                restApi, botConfig.Repository, trunkHeadChangesetId);

            return new BuildProperties
            {
                BranchName = branchName,
                TaskNumber = taskNumber,
                BranchHead = branchHeadChangeset.ChangesetId.ToString(),
                BranchHeadGuid = branchHeadChangeset.Guid.ToString(),
                ChangesetOwner = branchHeadChangeset.Owner,
                TrunkHead = trunkHeadChangeset.ChangesetId.ToString(),
                TrunkHeadGuid = trunkHeadChangeset.Guid.ToString(),
                RepSpec = string.Format("{0}@{1}", botConfig.Repository, botConfig.Server)
            };
        }

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
