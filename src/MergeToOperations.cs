using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class MergeToOperations
    {
        internal static bool TryMergeToShelve(
            RestApi restApi,
            Branch branch,
            string destinationBranch,
            MergeReport mergeReport,
            string comment,
            string taskNumber,
            TrunkBotConfiguration botConfig,
            out int shelveId)
        {
            shelveId = -1;

            MergeToResponse result = TrunkMergebotApi.MergeBranchTo(
                restApi, branch.Repository, branch.FullName, destinationBranch,
                comment, TrunkMergebotApi.MergeToOptions.CreateShelve);

            if (result.Status == MergeToResultStatus.AncestorNotFound ||
                result.Status == MergeToResultStatus.Conflicts ||
                result.Status == MergeToResultStatus.Error ||
                result.ChangesetNumber == 0)
            {
                BuildMergeReport.AddFailedMergeProperty(mergeReport, result.Status, result.Message);
                ChangeTaskStatus.SetTaskAsFailed(
                    restApi,
                    branch,
                    taskNumber,
                    string.Format(
                        "Can't merge branch {0}. Reason: {1}",
                        branch.FullName, result.Message),
                    botConfig);
                return false;
            }

            shelveId = result.ChangesetNumber;
            BuildMergeReport.AddSucceededMergeProperty(mergeReport, result.Status);

            if (result.Status == MergeToResultStatus.MergeNotNeeded)
            {
                ChangeTaskStatus.SetTaskAsMerged(
                    restApi,
                    branch,
                    taskNumber,
                    string.Format(
                        "Branch {0} was already merged to {1} (MergeNotNeeded).",
                        branch.FullName,
                        botConfig.TrunkBranch),
                    botConfig);
                return false;
            }

            return true;
        }

        internal static bool TryApplyShelve(
            RestApi restApi,
            Branch branch,
            string destinationBranch,
            MergeReport mergeReport,
            string comment,
            string taskNumber,
            int shelveId,
            TrunkBotConfiguration botConfig,
            out int csetId)
        {
            MergeToResponse mergeResult = TrunkMergebotApi.MergeShelveTo(
                restApi, branch.Repository, shelveId, destinationBranch,
                comment, TrunkMergebotApi.MergeToOptions.EnsureNoDstChanges);

            csetId = mergeResult.ChangesetNumber;
            BuildMergeReport.UpdateMergeProperty(mergeReport, mergeResult.Status, csetId);

            if (mergeResult.Status == MergeToResultStatus.OK)
                return true;

            if (mergeResult.Status == MergeToResultStatus.DestinationChanges)
            {
                // it should checkin the shelve only on the exact parent shelve cset.
                // if there are new changes in the trunk branch enqueue againg the task
                return false;
            }

            ChangeTaskStatus.SetTaskAsFailed(
                restApi,
                branch,
                taskNumber,
                string.Format(
                    "Can't merge branch {0}. Reason: {1}",
                    branch.FullName,
                    mergeResult.Message),
                botConfig);

            return false;
        }
    }
}
