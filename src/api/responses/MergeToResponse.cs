using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TrunkBot.Api.Responses
{
    public class MergeToResponse
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MergeToResultStatus Status { get; set; }
        public string Message { get; set; }
        public int ChangesetNumber { get; set; }
    }

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

    public class MergeToAllowedResponse
    {
        public string Result { get; set; }
    }
}
