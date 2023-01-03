using System;

namespace TrunkBot
{
    public class Review
    {
        public readonly string Repository;
        public readonly int ReviewId;
        public readonly int BranchId;
        public readonly string ReviewStatus;
        public readonly string ReviewTitle;

        public Review(
            string repository,
            int reviewId,
            int branchId,
            string reviewStatus,
            string reviewTitle)
        {
            Repository = repository;
            ReviewId = reviewId;
            BranchId = branchId;
            ReviewStatus = reviewStatus;
            ReviewTitle = reviewTitle;
        }

        internal bool IsDeleted()
        {
            return ReviewStatus != null && ReviewStatus.Trim().Equals(
                DELETED_STATUS, StringComparison.InvariantCultureIgnoreCase);
        }

        internal bool IsReviewed()
        {
            return ReviewStatus != null && ReviewStatus.Trim().Equals(
                REVIEWED_STATUS, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static string ParseStatusId(int parsedInt)
        {
            if (parsedInt == DELETED_STATUS_ID)
                return DELETED_STATUS;
            if (parsedInt == UNDER_REVIEW_STATUS_ID)
                return UNDER_REVIEW_STATUS;
            if (parsedInt == REVIEWED_STATUS_ID)
                return REVIEWED_STATUS;
            if (parsedInt == REWORK_REQUIRED_STATUS_ID)
                return REWORK_REQUIRED_STATUS;

            return string.Empty;
        }

        const int DELETED_STATUS_ID = -1;
        internal const int UNDER_REVIEW_STATUS_ID = 0;
        const int REVIEWED_STATUS_ID = 1;
        const int REWORK_REQUIRED_STATUS_ID = 2;

        const string DELETED_STATUS = "Deleted";
        const string UNDER_REVIEW_STATUS = "Under review";
        const string REVIEWED_STATUS = "Reviewed";
        const string REWORK_REQUIRED_STATUS = "Rework required";
    }
}
