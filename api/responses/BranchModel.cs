using System;

namespace TrunkBot.Api.Responses
{
    public class BranchModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string RepositoryId { get; set; }
        public int HeadChangeset { get; set; }
        public DateTime Date { get; set; }
        public string Owner { get; set; }
        public string Comment { get; set; }
        public string Type { get; set; }
    }
}
