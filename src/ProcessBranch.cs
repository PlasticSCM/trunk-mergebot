using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using TrunkBot.Configuration;
using TrunkBot.Labeling;
using MergeResultStatus = TrunkBot.MergeToOperations.Result.ResultStatus;
using BuildType = TrunkBot.TrunkMergebot.BuildInProgress.BuildType;

namespace TrunkBot
{
    internal static class ProcessBranch
    {
        internal enum Result
        {
            NotReady,
            Ok,
            Failed,
            QueueAgain,
            Cancelled
        };

        internal static async Task<Result> TryResumeProcessBranchInAfterCheckinBuildStage(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            IReportMerge reportMerge,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            TrunkBotConfiguration botConfig,
            string botName,
            ReviewsStorage reviewsStorage,
            int csetId,
            string buildId,
            int initBuildTime,
            CancellationToken cancellationToken)
        {
            string taskNumber = string.Empty;
            MergeReport mergeReport = null;
            try
            {
                taskNumber = GetTaskNumber(branch.FullName, botConfig.BranchPrefix);
                string repId = await repoApi.GetBranchRepId(
                    branch.Repository, branch.FullName, cancellationToken);
                mergeReport = BuildMergeReport.Build(repId, branch.Id);

                Result buildResult = await TryResumeAfterCheckinPlan(
                    notifier,
                    ci,
                    userProfile,
                    storeBuildInProgress,
                    branch,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    csetId,
                    buildId,
                    initBuildTime,
                    cancellationToken);

                if (buildResult == Result.Cancelled)
                    return buildResult;

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return buildResult;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                await ReportProcessBranchError(
                    ex,
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    botName,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    reviewsStorage);

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return Result.Failed;
            }
        }

        internal static async Task<Result> TryResumeProcessBranchInBuildStage(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            IReportMerge reportMerge,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            TrunkBotConfiguration botConfig,
            string botName,
            ReviewsStorage reviewStorage,
            int shelveId,
            List<MergeToXlinkChangeset> xlinkShelves,
            string buildId,
            int initBuildTime,
            CancellationToken cancellationToken)
        {
            string taskNumber = string.Empty;
            MergeReport mergeReport = null;
            try
            {
                taskNumber = GetTaskNumber(branch.FullName, botConfig.BranchPrefix);

                string repId = await repoApi.GetBranchRepId(
                    branch.Repository, branch.FullName, cancellationToken);
                mergeReport = BuildMergeReport.Build(repId, branch.Id);

                Result buildResult = await TryResumeBuildTask(
                    issueTracker,
                    notifier,
                    ci,
                    repoApi,
                    userProfile,
                    storeBuildInProgress,
                    branch,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    shelveId,
                    buildId,
                    initBuildTime,
                    reviewStorage,
                    cancellationToken);

                if (buildResult == Result.Cancelled)
                    return buildResult;

                if (buildResult == Result.Failed)
                {
                    ReportMerge(
                        reportMerge,
                        branch.Repository,
                        branch.FullName,
                        botName,
                        mergeReport);

                    return buildResult;
                }

                IssueInfo issueInfo =
                    await GetIssueInfo(issueTracker, taskNumber, botName, botConfig.Issues);

                string comment = GetComment(
                    branch.FullName, issueInfo != null ? issueInfo.Title : branch.Comment, botName);

                buildResult = await CheckinShelveAndPostActionsAfterSuccessfulBuild(
                    issueTracker,
                    notifier,
                    ci,
                    repoApi,
                    userProfile,
                    storeBuildInProgress,
                    branch,
                    botName,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    shelveId,
                    xlinkShelves,
                    comment,
                    reviewStorage,
                    cancellationToken);

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return buildResult;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                await ReportProcessBranchError(
                    ex,
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    botName,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    reviewStorage);

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return Result.Failed;
            }
        }

        internal static async Task<Result> TryProcessBranch(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            IReportMerge reportMerge,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            TrunkBotConfiguration botConfig,
            string botName,
            ReviewsStorage reviewsStorage,
            CancellationToken cancellationToken)
        {
            string taskNumber = null;
            MergeReport mergeReport = null;
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                mLog.InfoFormat("[{0}] Getting task number of branch {1} ...",
                    botName, branch.FullName);

                taskNumber = GetTaskNumber(branch.FullName, botConfig.BranchPrefix);
                if (!await IsTaskReady(
                        issueTracker,
                        taskNumber,
                        botConfig.Issues,
                        botConfig.Plastic.IsApprovedCodeReviewFilterEnabled,
                        branch.Repository,
                        branch.Id,
                        branch.FullName,
                        botName,
                        reviewsStorage,
                        cancellationToken))
                {
                    return Result.NotReady;
                }

                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                if (!await repoApi.IsMergeAllowed(
                        branch.Repository, branch.FullName, botConfig.TrunkBranch, cancellationToken))
                {
                    mLog.WarnFormat(
                        "[{0}] Branch {1} is not yet ready to be merged. " +
                        "Jumping to next branch in the queue...",
                        botName, branch.FullName);

                    return Result.NotReady;
                }

                mLog.InfoFormat("[{0}] Building the merge report of task {1} ...",
                    botName, taskNumber);

                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                string repId = await repoApi.GetBranchRepId(
                    branch.Repository, branch.FullName, cancellationToken);
                mergeReport = BuildMergeReport.Build(repId, branch.Id);

                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                IssueInfo issue = await GetIssueInfo(
                    issueTracker, taskNumber, botName, botConfig.Issues);

                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                if (issue != null)
                    BuildMergeReport.AddIssueProperty(mergeReport, issue.Title, issue.Url);

                string comment = GetComment(
                    branch.FullName, issue != null ? issue.Title : branch.Comment, botName);

                if (cancellationToken.IsCancellationRequested)
                    return Result.NotReady;

                mLog.InfoFormat(
                    "[{0}] Trying to shelve server-side-merge from {1} to {2}",
                    botName, branch.FullName, botConfig.TrunkBranch);

                MergeToOperations.Result mergeResult =
                    await MergeToOperations.TryMergeToShelve(
                        issueTracker,
                        notifier,
                        repoApi,
                        userProfile,
                        branch,
                        botConfig.TrunkBranch,
                        mergeReport,
                        comment,
                        taskNumber,
                        botConfig,
                        reviewsStorage);

                if (mergeResult.Status != MergeResultStatus.Succeed)
                {
                    ReportMerge(
                        reportMerge,
                        branch.Repository,
                        branch.FullName,
                        botName,
                        mergeReport);

                    return Result.Failed;
                }

                int shelveId = mergeResult.CreatedId;
                List<MergeToXlinkChangeset> xlinkShelves = mergeResult.CreatedXlinkChangesets;

                Dictionary<string, string> userDefBranchAttrsBeforeCheckin =
                    await GetUserDefBranchAttributesValues(
                        repoApi,
                        branch.Repository,
                        branch.FullName,
                        botConfig.CI?.BranchAttributeNamesToForwardBeforeCheckin);

                mLog.InfoFormat("[{0}] Testing branch {1} ...", botName, branch.FullName);

                Result buildResult = await TryBuildTask(
                    issueTracker,
                    notifier,
                    ci,
                    repoApi,
                    userProfile,
                    storeBuildInProgress,
                    branch,
                    mergeReport,
                    taskNumber,
                    shelveId,
                    xlinkShelves,
                    botConfig,
                    userDefBranchAttrsBeforeCheckin,
                    reviewsStorage,
                    cancellationToken);

                if (buildResult == Result.Cancelled)
                    return buildResult;

                if (buildResult == Result.Failed)
                {
                    DeleteShelves(repoApi, branch.Repository, botName, shelveId, xlinkShelves);

                    ReportMerge(
                        reportMerge,
                        branch.Repository,
                        branch.FullName,
                        botName,
                        mergeReport);

                    return buildResult;
                }

                buildResult = await CheckinShelveAndPostActionsAfterSuccessfulBuild(
                    issueTracker,
                    notifier,
                    ci,
                    repoApi,
                    userProfile,
                    storeBuildInProgress,
                    branch,
                    botName,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    shelveId,
                    xlinkShelves,
                    comment,
                    reviewsStorage,
                    cancellationToken);

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return buildResult;
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                await ReportProcessBranchError(
                    ex,
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    botName,
                    botConfig,
                    mergeReport,
                    taskNumber,
                    reviewsStorage);

                ReportMerge(
                    reportMerge,
                    branch.Repository,
                    branch.FullName,
                    botName,
                    mergeReport);

                return Result.Failed;
            }
        }

        static async Task<Result> CheckinShelveAndPostActionsAfterSuccessfulBuild(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            string botName,
            TrunkBotConfiguration botConfig,
            MergeReport mergeReport,
            string taskNumber,
            int shelveId,
            List<MergeToXlinkChangeset> xlinkShelves,
            string comment,
            ReviewsStorage reviewsStorage,
            CancellationToken cancellationToken)
        {
            mLog.InfoFormat(
                "[{0}] Checking-in shelved merged {1} from {2} to {3}",
                botName, shelveId, branch.FullName, botConfig.TrunkBranch);

            MergeToOperations.Result mergeResult =
                await MergeToOperations.TryApplyShelve(
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    botConfig.TrunkBranch,
                    mergeReport,
                    comment,
                    taskNumber,
                    shelveId,
                    botConfig,
                    reviewsStorage);

            DeleteShelves(repoApi, branch.Repository, botName, shelveId, xlinkShelves);

            if (mergeResult.Status != MergeResultStatus.Succeed)
            {
                return mergeResult.Status == MergeResultStatus.QueueAgain
                    ? Result.QueueAgain
                    : Result.Failed;
            }

            mLog.InfoFormat(
                "[{0}] Checkin: Created changeset {1} in branch {2}",
                botName, mergeResult.CreatedId, botConfig.TrunkBranch);

            mLog.InfoFormat(
                "[{0}] Setting branch {1} as 'integrated'...",
                botName, branch.FullName);

            await ChangeTaskStatus.SetTaskAsMerged(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                taskNumber,
                string.Format(
                    "Branch {0} was correctly merged to {1}.",
                    branch.FullName,
                    botConfig.TrunkBranch),
                botConfig,
                reviewsStorage);

            string labelName = string.Empty;
            if (botConfig.Plastic.IsAutoLabelEnabled &&
                !string.IsNullOrWhiteSpace(botConfig.Plastic.AutomaticLabelPattern))
            {
                labelName = await CreateLabel(
                    notifier,
                    repoApi,
                    userProfile,
                    mergeResult.CreatedId,
                    branch.FullName,
                    botConfig.TrunkBranch,
                    botConfig.Repository,
                    botConfig.Plastic.AutomaticLabelPattern,
                    mergeReport,
                    branch.Owner,
                    botConfig.Notifications);

                if (string.IsNullOrEmpty(labelName))
                    return Result.Failed;
            }

            if (!HasToRunPlanAfterTaskMerged(botConfig.CI))
                return Result.Ok;

            if (cancellationToken.IsCancellationRequested)
            {
                mLog.DebugFormat(
                    "[{0}] Operation cancelled for task {1} and branch {2}",
                    botName, taskNumber, branch.FullName);

                return Result.Failed;
            }

            Dictionary<string, string> userDefBranchAttrsAfterCheckin =
                await GetUserDefBranchAttributesValues(
                    repoApi,
                    branch.Repository,
                    branch.FullName,
                    botConfig.CI?.BranchAttributeNamesToForwardAfterCheckin);

            return await TryRunAfterCheckinPlan(
                notifier,
                ci,
                repoApi,
                userProfile,
                storeBuildInProgress,
                branch,
                mergeReport,
                taskNumber,
                mergeResult.CreatedId,
                labelName,
                botConfig,
                userDefBranchAttrsAfterCheckin,
                cancellationToken);
        }

        static async Task<Result> TryBuildTask(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            MergeReport mergeReport,
            string taskNumber,
            int shelveId,
            List<MergeToXlinkChangeset> xlinkShelves,
            TrunkBotConfiguration botConfig,
            Dictionary<string, string> userDefBranchAttrs,
            ReviewsStorage reviewsStorage,
            CancellationToken cancellationToken)
        {
            if (botConfig.CI == null)
            {
                mLog.InfoFormat(NO_CI_MESSAGE_FORMAT, taskNumber);
                return Result.Ok;
            }

            BuildProperties properties = await CreateBuildProperties(
                repoApi,
                taskNumber,
                branch.FullName,
                string.Empty,
                BuildProperties.StageValues.PRE_CHECKIN,
                userDefBranchAttrs,
                botConfig,
                cancellationToken);

            await ChangeTaskStatus.SetTaskAsTesting(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                taskNumber,
                string.Format("Starting to test branch {0}.", branch.FullName),
                botConfig);

            int iniTime = Environment.TickCount;

            TrunkMergebot.BuildInProgress buildInProgress =
                TrunkMergebot.BuildInProgress.FromShelve(
                    branch.Repository,
                    branch.FullName,
                    shelveId,
                    xlinkShelves,
                    BuildType.Build,
                    iniTime);

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Format("sh:{0}@{1}", shelveId, repSpec);
            string comment = string.Format("Building branch {0}", branch.FullName);

            BuildPlan.PlanResult buildResult = await BuildPlan.Build(
                ci,
                storeBuildInProgress,
                buildInProgress,
                botConfig.CI.Plug,
                botConfig.CI.PlanBranch,
                scmSpecToSwitchTo, 
                comment, 
                properties, 
                cancellationToken);

            if (buildResult == BuildPlan.PlanResult.Cancelled)
                return Result.Cancelled;

            return await PostBuildActions(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                mergeReport,
                taskNumber,
                buildResult,
                iniTime,
                botConfig,
                reviewsStorage) ? Result.Ok : Result.Failed;
        }

        async static Task<Result> TryResumeBuildTask(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            TrunkBotConfiguration botConfig,
            MergeReport mergeReport,
            string taskNumber,
            int shelveId,
            string execId,
            int iniBuildTime,
            ReviewsStorage reviewsStorage,
            CancellationToken cancellationToken)
        {
            if (botConfig.CI == null)
            {
                mLog.InfoFormat(NO_CI_MESSAGE_FORMAT, taskNumber);
                return Result.Ok;
            }

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Format("sh:{0}@{1}", shelveId, repSpec);
            string comment = string.Format("Building branch {0}", branch.FullName);

            BuildPlan.PlanResult buildResult = await BuildPlan.ResumeBuild(
                ci, storeBuildInProgress, botConfig.CI.Plug, botConfig.CI.PlanBranch, execId,
                scmSpecToSwitchTo, comment, cancellationToken);

            if (buildResult == BuildPlan.PlanResult.Cancelled)
                return Result.Cancelled;

            return await PostBuildActions(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                mergeReport,
                taskNumber,
                buildResult,
                iniBuildTime,
                botConfig,
                reviewsStorage) ? Result.Ok : Result.Failed;
        }

        static async Task<bool> PostBuildActions(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            MergeReport mergeReport,
            string taskNumber,
            BuildPlan.PlanResult buildResult,
            int iniBuildTime,
            TrunkBotConfiguration botConfig,
            ReviewsStorage reviewsStorage)
        {
            BuildMergeReport.AddBuildTimeProperty(mergeReport,
                Environment.TickCount - iniBuildTime);

            if (buildResult.Succeeded)
            {
                BuildMergeReport.AddSucceededBuildProperty(mergeReport, botConfig.CI.PlanBranch);
                return true;
            }

            BuildMergeReport.AddFailedBuildProperty(mergeReport,
                botConfig.CI.PlanBranch, buildResult.Explanation);

            await ChangeTaskStatus.SetTaskAsFailed(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                taskNumber,
                string.Format(
                    "Branch {0} build failed. \nExplanation: {1}",
                    branch.FullName,
                    buildResult.Explanation),
                botConfig,
                reviewsStorage);

            return false;
        }

        static async Task ReportProcessBranchError(
            Exception ex,
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string botName,
            TrunkBotConfiguration botConfig,
            MergeReport mergeReport,
            string taskNumber,
            ReviewsStorage reviewsStorage)
        {
            mLog.ErrorFormat(
                "[{0}] The attempt to process task {1} failed for branch {2}: {3}",
                botName, taskNumber, branch.FullName, ex.Message);

            mLog.DebugFormat(
                "StackTrace:{0}{1}", Environment.NewLine, ex.StackTrace);

            await ChangeTaskStatus.SetTaskAsFailed(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                taskNumber,
                string.Format(
                    "Can't process branch {0} because of an unexpected error: {1}.",
                    branch.FullName,
                    ex.Message),
                botConfig,
                reviewsStorage);

            BuildMergeReport.SetUnexpectedExceptionProperty(mergeReport, ex.Message);
        }

        static async Task<string> CreateLabel(
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            int csetId, 
            string branchFullName,
            string trunkBranchName,
            string repository, 
            string automaticLabelPattern,
            MergeReport mergeReport,
            string branchOwner,
            TrunkBotConfiguration.Notifier notificationsConfig)
        {
            AutomaticLabeler.Result result = null;
            try
            {
                result = await AutomaticLabeler.CreateLabel(
                    repoApi, csetId, repository, automaticLabelPattern, DateTime.Now);
            }
            catch (Exception e)
            {
                mLog.ErrorFormat(
                    "An error occurred labeling the merged branch {0} in changeset {1}@{2}: {3}",
                    branchFullName,
                    csetId, 
                    repository, 
                    e.Message);

                if (result == null)
                    result = new AutomaticLabeler.Result(false, string.Empty, e.Message);
            }

            string labelCreated = result.Name;

            BuildMergeReport.AddLabelProperty(
                mergeReport, result.IsSuccessful, result.Name, result.ErrorMessage);

            string message = result.IsSuccessful ?
                string.Format(
                    "Label {0} created successfully in {1} branch, changeset cs:{2}@{3}",
                    labelCreated, trunkBranchName, csetId, repository) :
                string.Format(
                    "Failed to create label after merging branch {0} " +
                    "in {1} branch, changeset cs:{2}@{3}. Error: {4}",
                    branchFullName, trunkBranchName, csetId, repository, result.ErrorMessage);

            await Notifier.NotifyTaskStatus(
                notifier, userProfile, branchOwner, message, notificationsConfig);
            return result.IsSuccessful ? labelCreated : string.Empty;
        }

        static bool HasToRunPlanAfterTaskMerged(
            TrunkBotConfiguration.ContinuousIntegration ciConfig)
        {
            if (ciConfig == null)
                return false;

            return !string.IsNullOrEmpty(ciConfig.PlanAfterCheckin);
        }

        static async Task<Result> TryRunAfterCheckinPlan(
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch, 
            MergeReport mergeReport, 
            string taskNumber, 
            int csetId,
            string labelName,
            TrunkBotConfiguration botConfig,
            Dictionary<string, string> userDefBranchAttrs,
            CancellationToken cancellationToken)
        {
            if (ci == null)
                return Result.Ok;

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Format("cs:{0}@{1}", csetId, repSpec);

            string comment = string.Format(
                "Running plan after merging branch {0}", branch.FullName);

            BuildProperties properties = await CreateBuildProperties(
                repoApi, 
                taskNumber, 
                branch.FullName, 
                labelName,
                BuildProperties.StageValues.POST_CHECKIN,
                userDefBranchAttrs,
                botConfig,
                CancellationToken.None); // we cannot cancel here

            int iniTime = Environment.TickCount;

            TrunkMergebot.BuildInProgress buildInProgress =
                TrunkMergebot.BuildInProgress.FromCset(
                    branch.Repository,
                    branch.FullName,
                    csetId,
                    BuildType.AfterCheckinBuild,
                    iniTime);

            BuildPlan.PlanResult buildResult = await BuildPlan.Build(
                ci,
                storeBuildInProgress,
                buildInProgress,
                botConfig.CI.Plug,
                botConfig.CI.PlanAfterCheckin,
                scmSpecToSwitchTo,
                comment,
                properties,
                cancellationToken);

            if (buildResult == BuildPlan.PlanResult.Cancelled)
                return Result.Cancelled;

            return await PostAfterCheckinPlanActions(
                notifier, userProfile, branch, mergeReport, buildResult, iniTime, botConfig)
                ? Result.Ok
                : Result.Failed;
        }

        static async Task<Result> TryResumeAfterCheckinPlan(
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IGetUserProfile userProfile,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            Branch branch,
            TrunkBotConfiguration botConfig,
            MergeReport mergeReport,
            string taskNumber,
            int csetId,
            string execId,
            int iniBuildTime,
            CancellationToken cancellationToken)
        {
            if (botConfig.CI == null)
            {
                mLog.InfoFormat(NO_CI_MESSAGE_FORMAT, taskNumber);
                return Result.Ok;
            }

            string repSpec = string.Format("{0}@{1}", branch.Repository, botConfig.Server);
            string scmSpecToSwitchTo = string.Format("cs:{0}@{1}", csetId, repSpec);

            string comment = string.Format(
                "Running plan after merging branch {0}", branch.FullName);

            BuildPlan.PlanResult buildResult = await BuildPlan.ResumeBuild(
                ci,
                storeBuildInProgress,
                botConfig.CI.Plug,
                botConfig.CI.PlanBranch,
                execId,
                scmSpecToSwitchTo,
                comment,
                cancellationToken);

            if (buildResult == BuildPlan.PlanResult.Cancelled)
                return Result.Cancelled;

            bool res = await PostAfterCheckinPlanActions(
                notifier,
                userProfile,
                branch,
                mergeReport,
                buildResult,
                iniBuildTime,
                botConfig);

            return res ? Result.Ok : Result.Failed;
        }

        static async Task<bool> PostAfterCheckinPlanActions(
            INotifierPlugService notifier,
            IGetUserProfile userProfile,
            Branch branch,
            MergeReport mergeReport,
            BuildPlan.PlanResult buildResult,
            int iniBuildTime,
            TrunkBotConfiguration botConfig)
        {
            BuildMergeReport.AddBuildTimeProperty(mergeReport,
                Environment.TickCount - iniBuildTime);

            string message;

            //TODO:shall we set any attr in trunk branch?
            if (buildResult.Succeeded)
            {
                BuildMergeReport.AddSucceededBuildProperty(
                    mergeReport, botConfig.CI.PlanAfterCheckin);

                message = string.Format(
                    "Plan execution after merging branch {0} was successful.",
                    branch.FullName);

                await Notifier.NotifyTaskStatus(
                    notifier, 
                    userProfile,
                    branch.Owner,
                    message, 
                    botConfig.Notifications);

                return true;
            }

            BuildMergeReport.AddFailedBuildProperty(
                mergeReport, botConfig.CI.PlanAfterCheckin, buildResult.Explanation);

            message = string.Format(
                "Plan execution failed after merging branch {0}.\nExplanation: {1}",
                branch.FullName,
                buildResult.Explanation);

            await Notifier.NotifyTaskStatus(
                notifier, userProfile, branch.Owner, message, botConfig.Notifications);

            return false;
        }

        static string GetTaskNumber(string branch, string branchPrefix)
        {
            string branchName = BranchSpec.GetName(branch);

            if (string.IsNullOrEmpty(branchPrefix))
                return branchName;

            if (branchName.StartsWith(branchPrefix,
                    StringComparison.InvariantCultureIgnoreCase))
                return branchName.Substring(branchPrefix.Length);

            return null;
        }

        static string GetComment(string branch, string taskTitle, string botName)
        {
            string comment = string.Format("{0}: merged {1}", botName, branch);

            if (!string.IsNullOrEmpty(taskTitle))
                comment += " : " + taskTitle;

            return comment;
        }

        static async Task<bool> IsTaskReady(
            IIssueTrackerPlugService issueTracker,
            string taskNumber,
            TrunkBotConfiguration.IssueTracker issuesConfig,
            bool bIsApprovedCodeReviewFilterEnabled,
            string branchRepository,
            int branchId,
            string branchFullName,
            string botName,
            ReviewsStorage reviewsStorage,
            CancellationToken cancellationToken)
        {
            if (taskNumber == null)
                return false;

            if (issuesConfig == null && !bIsApprovedCodeReviewFilterEnabled)
                return true;

            if (bIsApprovedCodeReviewFilterEnabled && 
                !AtLeastOneCodeReviewAndAllAreReviewed(
                    branchRepository, branchId, branchFullName, botName, reviewsStorage))
            {
                return false;
            }

            if (issuesConfig == null)
                return true;

            if (cancellationToken.IsCancellationRequested)
                return false;

            mLog.InfoFormat(
                "[{0}] Checking if issue tracker [{1}] is available...",
                botName, issuesConfig.Plug);
            
            if (!issueTracker.IsIssueTrackerConnected(issuesConfig.Plug))
            {
                mLog.WarnFormat(
                    "[{0}] Issue tracker [{1}] is NOT available...",
                    botName, issuesConfig.Plug);

                return false;
            }

            if (cancellationToken.IsCancellationRequested)
                return false;

            mLog.InfoFormat(
                "[{0}] Checking if task {1} is ready in the issue tracker [{2}].",
                botName, taskNumber, issuesConfig.Plug);

            try
            {
                string status = await issueTracker.GetIssueFieldValue(
                    issuesConfig.Plug,
                    issuesConfig.ProjectKey,
                    taskNumber,
                    issuesConfig.StatusField.Name);

                mLog.DebugFormat(
                    "[{0}] Issue tracker status for task [{1}]: expected [{2}], was [{3}]",
                    botName, taskNumber, issuesConfig.StatusField.ResolvedValue, status);

                return status == issuesConfig.StatusField.ResolvedValue;
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "[{0}] Cannot retrieve task {1} status from issue tracker: {2}",
                    botName, taskNumber, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}", Environment.NewLine, ex.StackTrace);

                return false;
            }
        }

        static bool AtLeastOneCodeReviewAndAllAreReviewed(
            string branchRepository, 
            int branchId, 
            string branchFullName, 
            string botName,
            ReviewsStorage reviewsStorage)
        {
            List<Review> branchReviews =
                reviewsStorage.GetBranchReviews(branchRepository, branchId);

            if (branchReviews == null || branchReviews.Count == 0)
            {
                mLog.InfoFormat(
                    "[{0}] Branch not ready. No code reviews found for branch {1}",
                    botName, branchFullName);

                return false;
            }

            foreach (Review branchReview in branchReviews)
            {
                if (!branchReview.IsReviewed())
                {
                    mLog.InfoFormat(
                        "[{0}] Branch not ready. " +
                        "Code review found for branch {1}, but it's not reviewed yet.",
                        botName, branchFullName);

                    return false;
                }
            }

            return true;
        }

        static async Task<IssueInfo> GetIssueInfo(
            IIssueTrackerPlugService issueTracker,
            string taskNumber,
            string botName,
            TrunkBotConfiguration.IssueTracker issuesConfig)
        {
            if (issueTracker == null || issuesConfig == null)
                return null;

            mLog.InfoFormat("[{0}] Obtaining task {1} title...", botName, taskNumber);

            string taskTittle = await issueTracker.GetIssueFieldValue(
                issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber, issuesConfig.TitleField);

            mLog.InfoFormat("[{0}] Obtaining task {1} URL...", botName, taskNumber);

            string taskUrl = await issueTracker.GetIssueUrl(
                issuesConfig.Plug, issuesConfig.ProjectKey,
                taskNumber);

            return new IssueInfo(taskTittle, taskUrl);
        }

        static async Task<Dictionary<string, string>> GetUserDefBranchAttributesValues(
            IRepositoryOperationsForMergebot repoApi, 
            string repoName,
            string branchName,
            string[] branchAttributeNamesToForward)
        {
            if (branchAttributeNamesToForward == null)
                return new Dictionary<string, string>(0);

            Dictionary<string, string> userDefBranchAttrs = 
                new Dictionary<string, string>(branchAttributeNamesToForward.Length);

            foreach (string attributeName in branchAttributeNamesToForward)
            {
                userDefBranchAttrs[attributeName] =
                    await repoApi.GetBranchAttributeValue(
                        repoName, 
                        branchName, 
                        attributeName);
            }

            return userDefBranchAttrs;
        }

        static void ReportMerge(
            IReportMerge reportMerge,
            string repository,
            string branchName,
            string botName,
            MergeReport mergeReport)
        {
            if (mergeReport == null)
                return;

            try
            {
                reportMerge.Report(botName, mergeReport);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "[{0}] Unable to report merge for branch '{1}' on repository '{2}': {3}",
                    botName, branchName, repository, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }
        }

        static void DeleteShelves(
            IRepositoryOperationsForMergebot repoApi,
            string branchRepository,
            string botName,
            int shelveId,
            List<MergeToXlinkChangeset> xlinkShelves)
        {
            SafeDeleteShelve(repoApi, branchRepository, botName, shelveId);

            if (xlinkShelves == null)
                return;

            foreach (MergeToXlinkChangeset changeset in xlinkShelves)
            {
                SafeDeleteShelve(
                    repoApi,
                    changeset.RepositoryName,
                    botName,
                    changeset.ChangesetId);
            }
        }

        static void SafeDeleteShelve(
            IRepositoryOperationsForMergebot repoApi,
            string repository,
            string botName,
            int shelveId)
        {
            if (shelveId == -1)
                return;

            try
            {
                repoApi.DeleteShelve(repository, shelveId);
            }
            catch (Exception ex)
            {
                mLog.ErrorFormat(
                    "[{0}] Unable to delete shelve {1} on repository '{2}': {3}",
                    botName, shelveId, repository, ex.Message);

                mLog.DebugFormat(
                    "StackTrace:{0}{1}",
                    Environment.NewLine, ex.StackTrace);
            }
        }

        static async Task<BuildProperties> CreateBuildProperties(
            IRepositoryOperationsForMergebot repoApi,
            string taskNumber,
            string branchName,
            string labelName,
            string buildStagePreCiOrPostCi,
            Dictionary<string, string> userDefBranchAttrs,
            TrunkBotConfiguration botConfig,
            CancellationToken cancellationToken)
        {
            int branchHeadChangesetId =
                await repoApi.GetBranchHead(botConfig.Repository, branchName, cancellationToken);
            ChangesetModel branchHeadChangeset = await repoApi.GetChangeset(
                botConfig.Repository, branchHeadChangesetId, cancellationToken);

            int trunkHeadChangesetId = await repoApi.GetBranchHead(
                botConfig.Repository, botConfig.TrunkBranch, cancellationToken);
            ChangesetModel trunkHeadChangeset = await repoApi.GetChangeset(
                botConfig.Repository, trunkHeadChangesetId, cancellationToken);

            return new BuildProperties
            {
                BranchName = branchName,
                TaskNumber = taskNumber,
                BranchHead = branchHeadChangeset.ChangesetId.ToString(),
                BranchHeadGuid = branchHeadChangeset.Guid.ToString(),
                ChangesetOwner = branchHeadChangeset.Owner,
                TrunkHead = trunkHeadChangeset.ChangesetId.ToString(),
                TrunkHeadGuid = trunkHeadChangeset.Guid.ToString(),
                RepSpec = string.Format("{0}@{1}", botConfig.Repository, botConfig.Server),
                LabelName = labelName,
                Stage = buildStagePreCiOrPostCi,
                UserDefinedBranchAttributes = userDefBranchAttrs
            };
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot");

        const string NO_CI_MESSAGE_FORMAT = "No Continuous Integration Plug was set " +
            "for this mergebot. Therefore, no build actions for task {0} will be performed.";

        class IssueInfo
        {
            internal IssueInfo(string title, string url)
            {
                Title = title;
                Url = url;
            }

            internal readonly string Title;
            internal readonly string Url;
        }
    }
}
