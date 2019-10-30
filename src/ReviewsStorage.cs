using System;
using System.Collections.Generic;
using System.IO;

using log4net;

using Newtonsoft.Json;

namespace TrunkBot
{
    public static class ReviewsStorage
    {
        public static void WriteReview(Review review, string filePath)
        {
            lock (mLock)
            {
                List<Review> storedReviews = LoadReviews(filePath);

                Review existingReview;

                if (Exists(
                    review.Repository, review.ReviewId, storedReviews, out existingReview))
                {
                    storedReviews.Remove(existingReview);
                }

                storedReviews.Add(review);

                WriteReviews(storedReviews, filePath);
            }
        }

        public static void DeleteReview(Review review, string filePath)
        {
            lock (mLock)
            {
                List<Review> storedReviews = LoadReviews(filePath);

                Review existingReview;

                if (!Exists(
                    review.Repository, review.ReviewId, storedReviews, out existingReview))
                {
                    return;
                }

                storedReviews.Remove(existingReview);
                WriteReviews(storedReviews, filePath);
            }
        }

        public static List<Review> GetBranchReviews(
            string branchRepository, string branchId, string filePath)
        {

            lock (mLock)
            {
                List<Review> result = new List<Review>();
                List<Review> storedReviews = LoadReviews(filePath);
                foreach(Review stored in storedReviews)
                {
                    if (!RepositoryNameComparer.IsSameName(stored.Repository, branchRepository))
                        continue;

                    if (!stored.BranchId.Equals(branchId, StringComparison.InvariantCulture))
                        continue;

                    result.Add(stored);
                }

                return result;
            }
        }

        public static void DeleteBranchReviews(
            string branchRepository, string branchId, string filePath)
        {
            lock (mLock)
            {
                List<Review> result = new List<Review>();
                List<Review> storedReviews = LoadReviews(filePath);

                for (int i = storedReviews.Count -1; i>= 0; i--)
                {
                    if (!RepositoryNameComparer.IsSameName(storedReviews[i].Repository, branchRepository))
                        continue;

                    if (!storedReviews[i].BranchId.Equals(branchId, StringComparison.InvariantCulture))
                        continue;

                    storedReviews.RemoveAt(i);
                }

                WriteReviews(storedReviews, filePath);
            }
        }

        static List<Review> LoadReviews(string filePath)
        {
            List<Review> reviews = new List<Review>();

            if (!File.Exists(filePath))
                return reviews;

            using (StreamReader file = new StreamReader(filePath))
            {
                JsonSerializer serializer = new JsonSerializer();
                reviews = (List<Review>)serializer.Deserialize(file, typeof(List<Review>));
            }

            if (reviews == null)
                reviews = new List<Review>();

            return reviews;
        }

        static void WriteReviews(List<Review> reviews, string filePath)
        {
            if (reviews == null)
                reviews = new List<Review>();

            try
            {
                using (StreamWriter fileWriter = new StreamWriter(filePath))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(fileWriter, reviews);
                }
            }
            catch (Exception e)
            {
                mLog.ErrorFormat("Error writing {0} reviews to [{1}]: {2}",
                    reviews.Count, filePath, e.Message);
            }
        }

        static bool Exists(
            string reviewRepo,
            string reviewId, 
            List<Review> storedReviews, 
            out Review existingReview)
        {
            existingReview = null;

            if (storedReviews == null || storedReviews.Count == 0)
                return false;

            foreach (Review candidate in storedReviews)
            {
                if (!RepositoryNameComparer.IsSameName(candidate.Repository, reviewRepo))
                    continue;

                if (!candidate.ReviewId.Equals(reviewId, StringComparison.InvariantCulture))
                    continue;
                
                existingReview = candidate;
                return true;
            }

            return false;
        }

        public static class Testing
        {
            public static List<Review> TestingLoadReviews(string filePath)
            {
                return LoadReviews(filePath);
            }
        }

        static readonly object mLock = new object();

        static readonly ILog mLog = LogManager.GetLogger("reviewstorage");
    }
}
