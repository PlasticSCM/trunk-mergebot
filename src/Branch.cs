namespace TrunkBot
{
    internal class Branch
    {
        internal readonly string Repository;
        internal readonly string Id;
        internal string FullName;
        internal readonly string Owner;
        internal readonly string Comment;

        internal Branch(
            string repository,
            string id,
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
}
