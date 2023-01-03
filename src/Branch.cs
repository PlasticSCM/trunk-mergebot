namespace TrunkBot
{
    public class Branch
    {
        public readonly string Repository;
        public readonly int Id;
        public string FullName;
        public readonly string Owner;
        public readonly string Comment;

        public Branch(
            string repository,
            int id,
            string fullName,
            string owner,
            string comment)
        {
            Repository = repository;
            Id = id;
            FullName = fullName;
            Owner = owner;
            Comment = comment;
        }
    }

    internal class BranchWithReview
    {
        internal Branch Branch;
        internal Review Review;
    }
}
