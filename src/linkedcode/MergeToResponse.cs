using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Codice.CM.Server.Devops
{
    public enum MergeToResultStatus : byte
    {
        OK = 0,
        AncestorNotFound = 1,
        MergeNotNeeded = 2,
        Conflicts = 3,
        DestinationChanges = 4,
        Error = 5,
        MultipleHeads = 6
    }    
    
    public class MergeToResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MergeToResultStatus Status { get; set; }
        public string Message { get; set; }
        public int ChangesetNumber { get; set; }
        public List<MergeToXlinkChangeset> XlinkChangesets { get; set; }
    }

    public class MergeToXlinkChangeset
    {
        public string RepositoryName;
        public int ChangesetId;
    }
}
