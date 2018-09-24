using System;

namespace TrunkBot.Api.Responses
{
    class ChangesetModel
    {
        public int Id { get; set; }
        public int ChangesetId { get; set; }
        public string RepositoryId { get; set; }
        public int ParentChangesetId { get; set; }
        public string Branch { get; set; }
        public DateTime Date { get; set; }
        public Guid Guid { get; set; }
        public string Owner { get; set; }
        public string Comment { get; set; }
        public string Type { get; set; }
    }
}
