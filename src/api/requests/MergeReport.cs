using System;
using System.Collections.Generic;

namespace TrunkBot.Api.Requests
{
    public class MergeReport
    {
        public class Entry
        {
            public string Text { get; set; }
            public string Link { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
        }

        public DateTime Timestamp { get; set; }
        public string RepositoryId { get; set; }
        public int BranchId { get; set; }
        public List<Entry> Properties { get; set; }
    }
}
