using System;
using System.Collections.Generic;

using log4net;

using TrunkBot.Api;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal static class ChangeTaskStatus
    {
        internal static void SetTaskAsTesting(
            RestApi restApi,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig)
        {
            try
            {
                if (!string.IsNullOrEmpty(botConfig.Plastic.StatusAttribute.TestingValue))
                {
                    TrunkMergebotApi.ChangeBranchAttribute(
                        restApi, branch.Repository, branch.FullName,
                        botConfig.Plastic.StatusAttribute.Name,
                        botConfig.Plastic.StatusAttribute.TestingValue);
                }

                if (taskNumber != null && botConfig.Issues != null &&
                    !string.IsNullOrEmpty(botConfig.Issues.StatusField.TestingValue))
                {
                    TrunkMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.TestingValue);
                }

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, message,
                    botConfig.Notifications);
            }
            catch (Exception ex)
            {
                Notifier.NotifyException(
                    restApi, branch, message,
                    "testing", ex, botConfig.Notifications);
            }
        }

        internal static void SetTaskAsFailed(
            RestApi restApi,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig,
            string codeReviewsStorageFile)
        {
            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                    SetBranchReviewsAsPending(restApi, branch.Repository, branch.Id, codeReviewsStorageFile);

                TrunkMergebotApi.ChangeBranchAttribute(
                    restApi, branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.FailedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    TrunkMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.FailedValue);
                }

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, message,
                    botConfig.Notifications);
            }
            catch (Exception ex)
            {
                Notifier.NotifyException(
                    restApi, branch, message,
                    "failed", ex, botConfig.Notifications);
            }
        }

        internal static void SetTaskAsMerged(
            RestApi restApi,
            Branch branch,
            string taskNumber,
            string message,
            TrunkBotConfiguration botConfig,
            string codeReviewsStorageFile)
        {
            try
            {
                if (botConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
                {
                    ReviewsStorage.DeleteBranchReviews(
                        branch.Repository, branch.Id, codeReviewsStorageFile);
                }

                TrunkMergebotApi.ChangeBranchAttribute(
                    restApi, branch.Repository, branch.FullName,
                    botConfig.Plastic.StatusAttribute.Name,
                    botConfig.Plastic.StatusAttribute.MergedValue);

                if (taskNumber != null && botConfig.Issues != null)
                {
                    TrunkMergebotApi.Issues.SetIssueField(
                        restApi, botConfig.Issues.Plug, botConfig.Issues.ProjectKey,
                        taskNumber, botConfig.Issues.StatusField.Name,
                        botConfig.Issues.StatusField.MergedValue);
                }

                Notifier.NotifyTaskStatus(
                    restApi, branch.Owner, message,
                    botConfig.Notifications);
            }
            catch (Exception ex)
            {
                Notifier.NotifyException(
                    restApi, branch, message,
                    "merged", ex, botConfig.Notifications);
            }
        }

        static void SetBranchReviewsAsPending(
            RestApi restApi, 
            string repoName,
            string branchId, 
            string codeReviewsStorageFile)
        {
            List<Review> branchReviews = ReviewsStorage.GetBranchReviews(
                repoName, branchId, codeReviewsStorageFile);

            foreach (Review review in branchReviews)
            {
                TrunkMergebotApi.CodeReviews.Update(
                    restApi,
                    repoName,
                    review.ReviewId,
                    Review.PENDING_STATUS_ID,
                    review.ReviewTitle);
            }
            
        }

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
