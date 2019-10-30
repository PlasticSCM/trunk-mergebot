using System;
using System.Collections.Generic;
using System.Threading;

using log4net;
using Newtonsoft.Json.Linq;

using TrunkBot.Api;
using TrunkBot.Configuration;
using TrunkBot.Messages;
using TrunkBot.WebSockets;

namespace TrunkBot
{
    internal class TrunkMergebot
    {
        internal TrunkMergebot(
            string restApiUrl,
            TrunkBotConfiguration trunkBotConfig,
            string branchesQueueFilePath,
            string codeReviewsTrackedFilePath,
            string botName)
        {
            mTrunkBotConfig = trunkBotConfig;
            mBranchesQueueFilePath = branchesQueueFilePath;
            mCodeReviewsTrackedFilePath = codeReviewsTrackedFilePath;
            mBotName = botName;

            mRestApi = new RestApi(restApiUrl, mTrunkBotConfig.UserApiKey);
        }

        internal void EnsurePlasticStatusAttributeExists()
        {
            if (FindQueries.ExistsAttributeName(
                mRestApi, 
                mTrunkBotConfig.Repository, 
                mTrunkBotConfig.Plastic.StatusAttribute.Name))
            {
                return;
            }

            if (TrunkMergebotApi.Attributes.CreateAttribute(
                mRestApi,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.Plastic.StatusAttribute.Name,
                "Attribute automatically created by trunk-bot: " + mBotName))
            {
                return;
            }

            throw new Exception(string.Format(
                "Trunkbot {0}: Unable to create " +
                "attribute name {1} on repository {2}. " +
                "Check the server log.",
                mBotName,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.Plastic.StatusAttribute.Name));
        }

        internal void LoadBranchesToProcess()
        {
            mLog.Info("Retrieving branches to process...");

            if (mTrunkBotConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
            {
                List<BranchWithReview> branchesWithReviews =
                    FindQueries.FindPendingBranchesWithReviews(
                        mRestApi,
                        mTrunkBotConfig.Repository,
                        mTrunkBotConfig.BranchPrefix ?? string.Empty,
                        mTrunkBotConfig.Plastic.StatusAttribute.Name,
                        mTrunkBotConfig.Plastic.StatusAttribute.MergedValue);

                HashSet<string> branchIdsProcessed = new HashSet<string>();
                List<Branch> branchesToEnqueue = new List<Branch>();

                foreach(BranchWithReview branchWithReview in branchesWithReviews)
                {
                    ReviewsStorage.WriteReview(
                        branchWithReview.Review, 
                        mCodeReviewsTrackedFilePath);

                    if (mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                        continue;

                    if (branchIdsProcessed.Contains(branchWithReview.Branch.Id))
                        continue;

                    branchIdsProcessed.Add(branchWithReview.Branch.Id);
                    branchesToEnqueue.Add(branchWithReview.Branch);
                }

                BranchesQueueStorage.WriteQueuedBranches(
                    branchesToEnqueue, mBranchesQueueFilePath);
            }

            if (!mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                return;

            List<Branch> branches = FindQueries.FindResolvedBranches(
                mRestApi,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.BranchPrefix ?? string.Empty,
                mTrunkBotConfig.Plastic.StatusAttribute.Name,
                mTrunkBotConfig.Plastic.StatusAttribute.ResolvedValue);

            BranchesQueueStorage.WriteQueuedBranches(branches, mBranchesQueueFilePath);
        }

        internal void OnEventReceived(object state)
        {
            //No new events are received while this event is processed so avoid process it here
            string message = (string)state;

            mLog.Debug(message);

            if (IsBranchAttributeChangedEvent(message))
            {
                ProcessBranchAttributeChangedEvent(message);
                return;
            }

            if (IsCodeReviewChangedEvent(message))
            {
                ProcessCodeReviewChangedEvent(message);
                return;
            }
        }

        bool IsBranchAttributeChangedEvent(string message)
        {
            return GetEventTypeFromMessage(message).Equals(
                WebSocketClient.BRANCH_ATTRIBUTE_CHANGED_TRIGGER_TYPE);
        }

        bool IsCodeReviewChangedEvent(string message)
        {
            return GetEventTypeFromMessage(message).Equals(
                WebSocketClient.CODE_REVIEW_CHANGED_TRIGGER_TYPE);
        }

        static string GetEventTypeFromMessage(string message)
        {
            try
            {
                JObject obj = JObject.Parse(message);
                return obj.Value<string>("event").ToString();
            }
            catch
            {
                mLog.ErrorFormat("Unable to parse incoming event: {0}", message);
                return string.Empty;
            }
        }

        void ProcessBranchAttributeChangedEvent(string message)
        {
            BranchAttributeChangeEvent e =
                ParseEvent.Parse<BranchAttributeChangeEvent>(message);

            if (!ShouldBeProcessed(
                e.Repository,
                e.BranchFullName,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.BranchPrefix))
            {
                return;
            }

            if (!IsRelevantAttribute(
                    e.AttributeName, mTrunkBotConfig.Plastic.StatusAttribute))
                return;

            lock (mSyncLock)
            {
                if (!IsAttributeValueResolved(
                        e.AttributeValue, mTrunkBotConfig.Plastic.StatusAttribute))
                {
                    BranchesQueueStorage.RemoveBranch(
                        e.Repository, e.BranchId, mBranchesQueueFilePath);
                    return;
                }

                EnqueueBranch(
                    mBranchesQueueFilePath,
                    e.Repository,
                    e.BranchId,
                    e.BranchFullName,
                    e.BranchOwner,
                    e.BranchComment);

                Monitor.Pulse(mSyncLock);
            }
        }

        void ProcessCodeReviewChangedEvent(string message)
        {
            CodeReviewChangeEvent e =
                ParseEvent.Parse<CodeReviewChangeEvent>(message);

            if (!ShouldBeProcessed(
                e.Repository,
                e.BranchFullName,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.BranchPrefix))
            {
                return;
            }

            Review review = new Review(
                e.Repository, e.CodeReviewId, e.BranchId, e.CodeReviewStatus, e.CodeReviewTitle);

            if (review.IsDeleted())
            {
                ReviewsStorage.DeleteReview(review, mCodeReviewsTrackedFilePath);

                if (!mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                {
                    List<Review> remainingBranchReviews = ReviewsStorage.GetBranchReviews(
                        e.Repository, e.BranchId, mCodeReviewsTrackedFilePath);

                    if (remainingBranchReviews != null && remainingBranchReviews.Count > 0)
                        return;

                    lock (mSyncLock)
                    {
                        BranchesQueueStorage.RemoveBranch(
                            e.Repository, e.BranchId, mBranchesQueueFilePath);
                    }
                }
                return;
            } 

            ReviewsStorage.WriteReview(review, mCodeReviewsTrackedFilePath);

            if (mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                return;

            lock (mSyncLock)
            {
                EnqueueBranch(
                    mBranchesQueueFilePath,
                    e.Repository,
                    e.BranchId,
                    e.BranchFullName,
                    e.BranchOwner,
                    e.BranchComment);

                Monitor.Pulse(mSyncLock);
            }
        }

        internal void ProcessBranches(object state)
        {
            while (true)
            {
                Branch branch;
                lock (mSyncLock)
                {
                    if (!BranchesQueueStorage.HasQueuedBranches(mBranchesQueueFilePath))
                    {
                        Monitor.Wait(mSyncLock, 1000);
                        continue;
                    }

                    branch = BranchesQueueStorage.DequeueBranch(mBranchesQueueFilePath);
                    branch.FullName = FindQueries.GetBranchName(
                        mRestApi, branch.Repository, branch.Id);
                }

                mLog.InfoFormat("Processing branch {0} attribute change...", branch.FullName);
                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    mRestApi, branch, mTrunkBotConfig, mBotName, mCodeReviewsTrackedFilePath);

                if (result == ProcessBranch.Result.Ok)
                {
                    mLog.InfoFormat("Branch {0} processing completed.", branch.FullName);
                    continue;
                }

                if (result == ProcessBranch.Result.Failed)
                {
                    mLog.InfoFormat("Branch {0} processing failed.", branch.FullName);
                    continue;
                }

                mLog.InfoFormat("Branch {0} is not ready. It will be queued again.", branch.FullName);

                lock (mSyncLock)
                {
                    if (BranchesQueueStorage.Contains(
                            branch.Repository, branch.Id,
                            mBranchesQueueFilePath))
                        continue;

                    BranchesQueueStorage.EnqueueBranch(
                        branch, mBranchesQueueFilePath);
                }

                Thread.Sleep(5000);
            }
        }

        static bool ShouldBeProcessed(
            string eventRepository,
            string eventBranchFullName,
            string repoTracked,
            string branchPrefixTracked)
        {
            if (!RepositoryNameComparer.IsSameName(eventRepository, repoTracked))
                return false;

            if (string.IsNullOrEmpty(branchPrefixTracked))
                return true;

            string branchName = BranchSpec.GetName(eventBranchFullName);

            return branchName.StartsWith(branchPrefixTracked,
                StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsRelevantAttribute(
            string attributeName,
            TrunkBotConfiguration.StatusProperty statusAttribute)
        {
            return attributeName.Equals(statusAttribute.Name,
                StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsAttributeValueResolved(
            string attributeValue,
            TrunkBotConfiguration.StatusProperty statusAttribute)
        {
            return attributeValue.Equals(statusAttribute.ResolvedValue,
                StringComparison.InvariantCultureIgnoreCase);
        }

        static void EnqueueBranch(
            string branchesQueueFilePath,
            string repository,
            string branchId,
            string branchFullName,
            string branchOwner,
            string branchComment)
        {
            if (BranchesQueueStorage.Contains(repository, branchId, branchesQueueFilePath))
                return;

            BranchesQueueStorage.EnqueueBranch(
                new Branch(repository, branchId, branchFullName, branchOwner, branchComment),
                branchesQueueFilePath);
        }

        readonly object mSyncLock = new object();

        readonly TrunkBotConfiguration mTrunkBotConfig;
        readonly string mBranchesQueueFilePath;
        readonly string mCodeReviewsTrackedFilePath;
        readonly string mBotName;
        readonly RestApi mRestApi;

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
