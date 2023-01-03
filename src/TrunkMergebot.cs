using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using TrunkBot.Configuration;

namespace TrunkBot
{
    internal class TrunkMergebot : INotifyMergebotTriggerActions, IMergebotService
    {
        internal class BuildInProgress
        {
            internal enum BuildType : byte
            {
                Build = 0,
                AfterCheckinBuild = 1
            };

            internal readonly string Repository;
            internal readonly string BranchName;
            internal readonly int CsetOrShelveId;
            internal readonly List<MergeToXlinkChangeset> XlinkShelves;
            internal readonly BuildType BuildStage;
            internal readonly int IniBuildTime;

            internal string BuildId;

            internal static BuildInProgress FromCset(
                string repository,
                string branchName,
                int csetId,
                BuildType stage,
                int iniBuildTime)
            {
                return new BuildInProgress(
                    repository,
                    branchName,
                    csetId,
                    null,
                    stage,
                    iniBuildTime);
            }

            internal static BuildInProgress FromShelve(
                string repository,
                string branchName,
                int shelveId,
                List<MergeToXlinkChangeset> xlinkShelves,
                BuildType stage,
                int iniBuildTime)
            {
                return new BuildInProgress(
                    repository,
                    branchName,
                    shelveId,
                    xlinkShelves,
                    stage,
                    iniBuildTime);
            }

            internal BuildInProgress(
                string repository,
                string branchName,
                int csetOrShelveId,
                List<MergeToXlinkChangeset> xlinkShelves,
                BuildType stage,
                int iniBuildTime)
            {
                Repository = repository;
                BranchName = branchName;
                CsetOrShelveId = csetOrShelveId;
                XlinkShelves = xlinkShelves;
                BuildStage = stage;
                IniBuildTime = iniBuildTime;
            }
        }

        internal interface IStoreBuildInProgress
        {
            BuildInProgress Load();
            void Save(BuildInProgress buildInProgress);
            void Delete();
        }

        internal TrunkMergebot(
            IIssueTrackerPlugService issueTracker,
            INotifierPlugService notifier,
            IContinuousIntegrationPlugService ci,
            IRepositoryOperationsForMergebot repoApi,
            IGetUserProfile userProfile,
            IReportMerge reportMerge,
            IStoreBuildInProgress storeBuildInProgress,
            TrunkBotConfiguration trunkBotConfig,
            string botName)
        {
            mTrunkBotConfig = trunkBotConfig;
            mBotName = botName;

            mIssueTracker = issueTracker;
            mNotifier = notifier;
            mCi = ci;
            mRepoApi = repoApi;
            mUserProfile = userProfile;
            mReportMerge = reportMerge;
            mStoreBuildInProgress = storeBuildInProgress;
        }

        async Task INotifyMergebotTriggerActions.NotifyBranchAttributeChanged(
            string repoName,
            int branchId,
            string branchName,
            string attributeName,
            string attributeValue,
            string branchOwner,
            string branchComment)
        {
            if (!ShouldBeProcessed(
                    repoName,
                    branchName,
                    mTrunkBotConfig.Repository,
                    mTrunkBotConfig.BranchPrefix))
            {
                return;
            }

            if (!IsRelevantAttribute(
                    attributeName, mTrunkBotConfig.Plastic.StatusAttribute))
                return;

            if (!IsAttributeValueResolved(
                    attributeValue, mTrunkBotConfig.Plastic.StatusAttribute))
            {
                mBranchesStorage.RemoveBranch(repoName, branchId);
                return;
            }

            mBranchesStorage.EnqueueBranchIfNotAlreadyAdded(
                new Branch(repoName, branchId, branchName, branchOwner, branchComment));

            mLog.DebugFormat("[{0}] Branch {1} was queued (attribute updated)",
                mBotName, branchName);
        }

        async Task INotifyMergebotTriggerActions.NotifyCodeReviewStatusChanged(
            string repoName,
            int branchId,
            string branchName,
            string branchOwner,
            string branchComment,
            int reviewId,
            string reviewTitle,
            string reviewStatus)
        {
            if (!ShouldBeProcessed(
                    repoName,
                    branchName,
                    mTrunkBotConfig.Repository,
                    mTrunkBotConfig.BranchPrefix))
            {
                return;
            }

            Review review = new Review(
                repoName,
                reviewId,
                branchId,
                reviewStatus,
                reviewTitle);

            if (review.IsDeleted())
            {
                mReviewsStorage.DeleteReview(review);

                if (!mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                {
                    List<Review> remainingBranchReviews = mReviewsStorage.GetBranchReviews(
                        repoName, branchId);

                    if (remainingBranchReviews != null && remainingBranchReviews.Count > 0)
                        return;

                    mBranchesStorage.RemoveBranch(repoName, branchId);
                }

                return;
            }

            mReviewsStorage.AddReview(review);

            if (mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                return;

            if (!review.IsReviewed())
                return;

            mBranchesStorage.EnqueueBranchIfNotAlreadyAdded(
                new Branch(repoName, branchId, branchName, branchOwner, branchComment));

            mLog.DebugFormat("[{0}] Branch {1} was queued (code review updated)",
                mBotName, branchName);

            mEventSyncSemaphore.Release();
        }

        Task INotifyMergebotTriggerActions.NotifyNewChangesets(string repoName, string branchName)
        {
            return Task.CompletedTask;
        }

        async Task IMergebotService.Initialize()
        {
            string attributeName = mTrunkBotConfig.Plastic.StatusAttribute.Name;
            if (!(await mRepoApi.ExistsAttributeName(mTrunkBotConfig.Repository, attributeName)))
            {
                string attributeComment = AttributeComment.Build(
                    mTrunkBotConfig.Plastic.StatusAttribute.GetValues(), mBotName);

                if (!(await mRepoApi.TryCreateAttribute(mTrunkBotConfig.Repository, attributeName,
                        attributeComment)))
                {
                    string message = string.Format(
                        "[{0}] cannot be started it wasn't able to configure the " +
                        "required plastic status attribute [{1}] (rep:{2}) for its proper working.",
                        mBotName, attributeName, mTrunkBotConfig.Repository);

                    mLog.Fatal(message);
                    throw new Exception(message);
                }
            }

            try
            {
                await LoadBranchesToProcess();
            }
            catch (Exception e)
            {
                mLog.FatalFormat(
                    "[{0}] cannot be started because it couldn't load " +
                    "the branches to process on startup. Reason: {1}", mBotName, e.Message);
                mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }
        }

        async Task IMergebotService.Start()
        {
            try
            {
                await ProcessBranches();
                mLog.InfoFormat("[{0}] was stopped properly.", mBotName);
            }
            catch (OperationCanceledException)
            {
                // nothing to do, the mergebot was requested to stop
                mLog.InfoFormat("[{0}] was stopped properly.", mBotName);
            }
            catch (Exception e)
            {
                mLog.FatalFormat("[{0}] stopped working. Error: {1}", mBotName, e.Message);
                mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
            }
        }

        async Task ProcessBranches()
        {
            if (mCancelTokenSource.Token.IsCancellationRequested)
                return;

            BuildInProgress buildInProgress =
                mStoreBuildInProgress != null ? mStoreBuildInProgress.Load() : null;

            if (buildInProgress != null)
            {
                Branch branch = await mRepoApi.GetBranch(
                    buildInProgress.Repository, buildInProgress.BranchName, mCancelTokenSource.Token);

                if (buildInProgress.BuildStage == BuildInProgress.BuildType.Build)
                {
                    await ProcessBranch.TryResumeProcessBranchInBuildStage(
                        mIssueTracker,
                        mNotifier,
                        mCi,
                        mRepoApi,
                        mUserProfile,
                        mReportMerge,
                        mStoreBuildInProgress,
                        branch,
                        mTrunkBotConfig,
                        mBotName,
                        mReviewsStorage,
                        buildInProgress.CsetOrShelveId,
                        buildInProgress.XlinkShelves,
                        buildInProgress.BuildId,
                        buildInProgress.IniBuildTime,
                        mCancelTokenSource.Token);
                }

                if (mCancelTokenSource.Token.IsCancellationRequested)
                    return;

                if (buildInProgress.BuildStage == BuildInProgress.BuildType.AfterCheckinBuild)
                {
                    await ProcessBranch.TryResumeProcessBranchInAfterCheckinBuildStage(
                        mIssueTracker,
                        mNotifier,
                        mCi,
                        mRepoApi,
                        mUserProfile,
                        mReportMerge,
                        mStoreBuildInProgress,
                        branch,
                        mTrunkBotConfig,
                        mBotName,
                        mReviewsStorage,
                        buildInProgress.CsetOrShelveId,
                        buildInProgress.BuildId,
                        buildInProgress.IniBuildTime,
                        mCancelTokenSource.Token);
                }
            }

            Dictionary<int, int> branchesNotProcessedWaitCache = new Dictionary<int, int>();
            bool bWaitForNewBranches = false;
            while (true)
            {
                if (mCancelTokenSource.Token.IsCancellationRequested)
                    return;

                //avoid busy wait when a task is not ready and queued again
                await Task.Delay(1000, mCancelTokenSource.Token);

                if (bWaitForNewBranches)
                    await mEventSyncSemaphore.WaitAsync(TimeSpan.FromSeconds(1));

                Branch branch = mBranchesStorage.PeekBranch();
                if (branch == null)
                {
                    bWaitForNewBranches = true;
                    continue;
                }

                int waitSeconds;
                if (branchesNotProcessedWaitCache.TryGetValue(branch.Id, out waitSeconds))
                {
                    // add extra wait time when processing a branch that
                    // was already checked and was not ready yet.
                    await Task.Delay(waitSeconds * 1000, mCancelTokenSource.Token);
                }

                branch = mBranchesStorage.DequeueBranch();

                bWaitForNewBranches = false;
                branch.FullName = await mRepoApi.GetBranchName(
                    branch.Repository, branch.Id, mCancelTokenSource.Token);

                mLog.InfoFormat("[{0}] Processing branch {1} attribute change...",
                    mBotName, branch.FullName);
                ProcessBranch.Result result = await ProcessBranch.TryProcessBranch(
                    mIssueTracker,
                    mNotifier,
                    mCi,
                    mRepoApi,
                    mUserProfile,
                    mReportMerge,
                    mStoreBuildInProgress,
                    branch,
                    mTrunkBotConfig,
                    mBotName,
                    mReviewsStorage,
                    mCancelTokenSource.Token);

                if (result == ProcessBranch.Result.Ok)
                {
                    mLog.InfoFormat("[{0}] Branch {1} processing completed.",
                        mBotName, branch.FullName);
                    branchesNotProcessedWaitCache.Remove(branch.Id);
                    continue;
                }

                if (result == ProcessBranch.Result.Failed)
                {
                    mLog.InfoFormat("[{0}] Branch {1} processing failed.",
                        mBotName, branch.FullName);
                    branchesNotProcessedWaitCache.Remove(branch.Id);
                    continue;
                }

                if (result == ProcessBranch.Result.Cancelled)
                {
                    mLog.InfoFormat("[{0}] Branch {1} processing was cancelled.",
                        mBotName, branch.FullName);
                    branchesNotProcessedWaitCache.Remove(branch.Id);
                    continue;
                }

                if (!branchesNotProcessedWaitCache.TryGetValue(branch.Id, out waitSeconds))
                    waitSeconds = 0;

                branchesNotProcessedWaitCache[branch.Id] =
                    waitSeconds < 60 ? waitSeconds + 10 : waitSeconds;

                mLog.InfoFormat(
                    "[{0}] Branch {1} will be queued again (Status={2}).",
                    mBotName,
                    branch.FullName,
                    result.ToString());


                if (result == ProcessBranch.Result.QueueAgain)
                {
                    mBranchesStorage.EnqueueBranchOnTop(branch);
                    continue;
                }

                mBranchesStorage.EnqueueBranchIfNotAlreadyAdded(branch);
            }
        }

        void IMergebotService.Stop()
        {
            mCancelTokenSource.Cancel();
        }

        async Task LoadBranchesToProcess()
        {
            int ini = Environment.TickCount;
            mLog.InfoFormat("[{0}] Retrieving branches to process...", mBotName);

            if (mTrunkBotConfig.Plastic.IsApprovedCodeReviewFilterEnabled)
            {
                List<BranchWithReview> branchesWithReviews =
                    await mRepoApi.FindPendingBranchesWithReviews(
                        mTrunkBotConfig.Repository,
                        mTrunkBotConfig.BranchPrefix ?? string.Empty,
                        mTrunkBotConfig.Plastic.StatusAttribute.Name,
                        mTrunkBotConfig.Plastic.StatusAttribute.MergedValue,
                        CancellationToken.None);

                HashSet<int> branchIdsProcessed = new HashSet<int>();
                List<Branch> branchesToEnqueue = new List<Branch>();

                foreach (BranchWithReview branchWithReview in branchesWithReviews)
                {
                    mReviewsStorage.AddReview(branchWithReview.Review);

                    if (mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
                        continue;

                    if (branchIdsProcessed.Contains(branchWithReview.Branch.Id))
                        continue;

                    branchIdsProcessed.Add(branchWithReview.Branch.Id);
                    branchesToEnqueue.Add(branchWithReview.Branch);
                }

                mBranchesStorage.AddQueuedBranches(branchesToEnqueue);
            }

            if (!mTrunkBotConfig.Plastic.IsBranchAttrFilterEnabled)
            {
                mLog.InfoFormat("[{0}] Branches to process retrieved in {1} ms.",
                    mBotName, Environment.TickCount - ini);
                return;
            }

            List<Branch> branches = await mRepoApi.FindResolvedBranches(
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.BranchPrefix ?? string.Empty,
                mTrunkBotConfig.Plastic.StatusAttribute.Name,
                mTrunkBotConfig.Plastic.StatusAttribute.ResolvedValue,
                CancellationToken.None);

            mBranchesStorage.AddQueuedBranches(branches);

            mLog.InfoFormat("[{0}] Branches to process retrieved in {1} ms.",
                mBotName, Environment.TickCount - ini);
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

        readonly SemaphoreSlim mEventSyncSemaphore = new SemaphoreSlim(1);

        readonly TrunkBotConfiguration mTrunkBotConfig;
        readonly string mBotName;

        readonly IIssueTrackerPlugService mIssueTracker;
        readonly INotifierPlugService mNotifier;
        readonly IContinuousIntegrationPlugService mCi;
        readonly IRepositoryOperationsForMergebot mRepoApi;
        readonly IGetUserProfile mUserProfile;
        readonly IReportMerge mReportMerge;
        readonly IStoreBuildInProgress mStoreBuildInProgress;

        readonly CancellationTokenSource mCancelTokenSource = new CancellationTokenSource();

        readonly ReviewsStorage mReviewsStorage = new ReviewsStorage();
        readonly BranchesQueueStorage mBranchesStorage = new BranchesQueueStorage();

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot");
    }
}