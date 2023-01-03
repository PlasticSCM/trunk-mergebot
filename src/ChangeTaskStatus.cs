using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;

using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class ChangeTaskStatus
    {
        internal static async Task SetTaskAsTesting(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig)
        {
            try
            {
                if (!string.IsNullOrEmpty(botConfig.Plastic.StatusAttribute.TestingValue))
                {
                    await repoApi.ChangeBranchAttributeValue(
                        branch.Repository, branch.FullName,
                        botConfig.Plastic.StatusAttribute.Name,
                        botConfig.Plastic.StatusAttribute.TestingValue);
                }

                if (taskNumber != null && botConfig.Issues != null &&
                    !string.IsNullOrEmpty(botConfig.Issues.StatusField.TestingValue))
                {
                    await issueTracker.SetIssueFieldValue(
                        botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.TestingValue);
                }

                await Notifier.NotifyTaskStatus(
                    notifier, userProfile, branch.Owner, message, botConfig.Notifications);
            }
            catch (Exception ex)
            {
                await Notifier.NotifyException(
                    notifier, userProfile, branch, message, "testing", ex, botConfig.Notifications);
            }
        }

        internal static async Task SetTaskAsFailed(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig,
            ReviewsStorage reviewsStorage)
        {
            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                    SetBranchReviewsAsUnderReview(
                        repoApi, branch.Repository, branch.Id, reviewsStorage);

                await repoApi.ChangeBranchAttributeValue(
                    branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.FailedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    await issueTracker.SetIssueFieldValue(
                        botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.FailedValue);
                }

                await Notifier.NotifyTaskStatus(
                    notifier, userProfile, branch.Owner, message, botConfig.Notifications);
            }
            catch (Exception ex)
            {
                await Notifier.NotifyException(
                    notifier, userProfile, branch, message, "failed", ex, botConfig.Notifications);
            }
        }

        internal static async Task SetTaskAsMerged(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig,
            ReviewsStorage reviewsStorage)
        {
            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                {
                    reviewsStorage.DeleteBranchReviews(branch.Repository, branch.Id);
                }

                await repoApi.ChangeBranchAttributeValue(
                    branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.MergedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    await issueTracker.SetIssueFieldValue(
                        botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.MergedValue);
                }

                await Notifier.NotifyTaskStatus(
                    notifier, userProfile, branch.Owner, message, botConfig.Notifications);
            }
            catch (Exception ex)
            {
                await Notifier.NotifyException(
                    notifier, userProfile, branch, message, "merged", ex, botConfig.Notifications);
            }
        }

        static void SetBranchReviewsAsUnderReview(
            IRepositoryOperationsForMergebot repoApi, 
            string repoName,
            int branchId, 
            ReviewsStorage reviewsStorage)
        {
            List<Review> branchReviews = reviewsStorage.GetBranchReviews(repoName, branchId);

            foreach (Review review in branchReviews)
            {
                repoApi.UpdateCodeReview(
                    repoName,
                    review.ReviewId,
                    Review.UNDER_REVIEW_STATUS_ID,
                    review.ReviewTitle);
            }
            
        }
    }
}
