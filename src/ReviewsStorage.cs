using System;
using System.Collections.Generic;
using System.IO;

using Codice.LogWrapper;

using Newtonsoft.Json;

namespace TrunkBot
{
    public class ReviewsStorage
    {
        public void AddReview(Review review)
        {
            lock (mLock)
            {
                Review existingReview;
                if (Exists(review.Repository, review.ReviewId, mReviews, out existingReview))
                    mReviews.Remove(existingReview);

                mReviews.Add(review);
            }
        }

        public void DeleteReview(Review review)
        {
            lock (mLock)
            {
                Review existingReview;
                if (!Exists(review.Repository, review.ReviewId, mReviews, out existingReview))
                    return;

                mReviews.Remove(existingReview);
            }
        }

        public List<Review> GetBranchReviews(string branchRepository, int branchId)
        {
            lock (mLock)
            {
                List<Review> result = new List<Review>();
                foreach(Review stored in mReviews)
                {
                    if (stored.BranchId != branchId)
                        continue;

                    if (!RepositoryNameComparer.IsSameName(stored.Repository, branchRepository))
                        continue;

                    result.Add(stored);
                }

                return result;
            }
        }

        public void DeleteBranchReviews(
            string branchRepository, int branchId)
        {
            lock (mLock)
            {
                for (int i = mReviews.Count -1; i>= 0; i--)
                {
                    if (mReviews[i].BranchId != branchId)
                        continue;

                    if (!RepositoryNameComparer.IsSameName(mReviews[i].Repository, branchRepository))
                        continue;

                    mReviews.RemoveAt(i);
                }
            }
        }

        static bool Exists(
            string reviewRepo,
            int reviewId, 
            List<Review> reviews,
            out Review existingReview)
        {
            existingReview = null;

            if (reviews == null || reviews.Count == 0)
                return false;

            foreach (Review candidate in reviews)
            {
                if (candidate.ReviewId != reviewId)
                    continue;

                if (!RepositoryNameComparer.IsSameName(candidate.Repository, reviewRepo))
                    continue;

                existingReview = candidate;
                return true;
            }

            return false;
        }

        readonly List<Review> mReviews = new List<Review>();
        readonly object mLock = new object();
    }
}
