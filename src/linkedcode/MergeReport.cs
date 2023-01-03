using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Codice.CM.Server.Devops
{
    public class MergeReport
    {
        public class Entry
        {
            [JsonProperty("text")]
            public string Text { get; set; }

            [JsonProperty("link")]
            public string Link { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("value")]
            public string Value { get; set; }
        }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonProperty("repositoryId", Required = Required.DisallowNull)]
        public string RepositoryId { get; set; }

        [JsonProperty("branchId")]
        public int BranchId { get; set; }

        [JsonProperty("properties")]
        public List<Entry> Properties { get; set; }
    }
}
