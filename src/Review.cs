using System;

namespace TrunkBot
{
    public class Review
    {
        public readonly string Repository;
        public readonly string ReviewId;
        public readonly string BranchId;
        public readonly string ReviewStatus;
        public readonly string ReviewTitle;

        public Review(
            string repository,
            string reviewId,
            string branchId,
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

        internal bool IsApproved()
        {
            return ReviewStatus != null && ReviewStatus.Trim().Equals(
                APPROVED_STATUS, StringComparison.InvariantCultureIgnoreCase);
        }

        internal static string ParseStatusId(int parsedInt)
        {
            if (parsedInt == -1)
                return DELETED_STATUS;
            if (parsedInt == 0)
                return PENDING_STATUS;
            if (parsedInt == 1)
                return APPROVED_STATUS;
            if (parsedInt == 2)
                return DISCARDED_STATUS;

            return string.Empty;
        }

        internal const int PENDING_STATUS_ID = 0;

        const string DELETED_STATUS = "Deleted";
        const string PENDING_STATUS = "Pending";
        const string APPROVED_STATUS = "Approved";
        const string DISCARDED_STATUS = "Discarded";
    }
}
