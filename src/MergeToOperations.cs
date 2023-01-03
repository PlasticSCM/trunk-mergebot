using System.Collections.Generic;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class MergeToOperations
    {
        internal class Result
        {
            internal enum ResultStatus
            {
                Failed,
                Succeed,
                QueueAgain,
            }

            internal readonly ResultStatus Status;
            internal readonly int CreatedId;
            internal readonly List<MergeToXlinkChangeset> CreatedXlinkChangesets;

            internal static Result BuildSucceeded(
                int createdId, List<MergeToXlinkChangeset> createdXlinkChangesets)
            {
                return new Result(ResultStatus.Succeed, createdId, createdXlinkChangesets);
            }

            internal static Result BuildFailed()
            {
                return new Result(ResultStatus.Failed, -1, null);
            }

            Result(
                ResultStatus status,
                int createdId,
                List<MergeToXlinkChangeset> createdXlinkChangesets)
            {
                Status = status;
                CreatedId = createdId;
                CreatedXlinkChangesets = createdXlinkChangesets;
            }

            internal static Result BuildQueueAgain()
            {
                return new Result(ResultStatus.QueueAgain, -1, null);
            }
        }

        internal static async Task<Result> TryMergeToShelve(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string destinationBranch,
            MergeReport mergeReport,
            string comment,
            string taskNumber,
            TrunkBotConfiguration botConfig,
            ReviewsStorage reviewsStorage)
        {
            MergeToResponse result = await repoApi.MergeBranchTo(
                branch.Repository, branch.FullName, destinationBranch,
                comment, MergeToOptions.CreateShelve);

            string message;

            if (result.Status == MergeToResultStatus.MergeNotNeeded)
            {
                message = string.Format("Branch {0} was already merged to {1} (MergeNotNeeded).",
                    branch.FullName, botConfig.TrunkBranch);
                mLog.Debug(message);

                await ChangeTaskStatus.SetTaskAsMerged(
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    taskNumber,
                    message,
                    botConfig,
                    reviewsStorage);
                return Result.BuildFailed();
            }

            if (result.Status == MergeToResultStatus.AncestorNotFound ||
                result.Status == MergeToResultStatus.Conflicts ||
                result.Status == MergeToResultStatus.Error ||
                result.ChangesetNumber == 0)
            {
                message = string.Format("Can't merge branch {0}. Reason: {1}",
                    branch.FullName, result.Message);
                mLog.Debug(message);

                BuildMergeReport.AddFailedMergeProperty(mergeReport, result.Status, result.Message);
                await ChangeTaskStatus.SetTaskAsFailed(
                    issueTracker,
                    notifier,
                    repoApi,
                    userProfile,
                    branch,
                    taskNumber,
                    message,
                    botConfig,
                    reviewsStorage);
                return Result.BuildFailed();;
            }

            BuildMergeReport.AddSucceededMergeProperty(mergeReport, result.Status);
            return Result.BuildSucceeded(result.ChangesetNumber, result.XlinkChangesets);
        }

        internal static async Task<Result> TryApplyShelve(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string destinationBranch,
            MergeReport mergeReport,
            string comment,
            string taskNumber,
            int shelveId,
            TrunkBotConfiguration botConfig,
            ReviewsStorage reviewsStorage)
        {
            MergeToResponse mergeResult = await repoApi.MergeShelveTo(
                branch.Repository, shelveId, destinationBranch,
                comment, MergeToOptions.EnsureNoDstChanges);

            int csetId = mergeResult.ChangesetNumber;
            BuildMergeReport.UpdateMergeProperty(mergeReport, mergeResult.Status, csetId);

            if (mergeResult.Status == MergeToResultStatus.OK)
                return Result.BuildSucceeded(csetId, mergeResult.XlinkChangesets);

            string message = string.Format("Can't merge branch {0}. Reason: {1}",
                branch.FullName, mergeResult.Message);
            mLog.Debug(message);

            if (mergeResult.Status == MergeToResultStatus.DestinationChanges
                && botConfig.QueueAgainOnFail)
            {
                return Result.BuildQueueAgain();
            }

            await ChangeTaskStatus.SetTaskAsFailed(
                issueTracker,
                notifier,
                repoApi,
                userProfile,
                branch,
                taskNumber,
                message,
                botConfig,
                reviewsStorage);

            return Result.BuildFailed();
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-MergeToOperations");
    }
}
