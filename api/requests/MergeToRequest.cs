using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TrunkBot.Api.Requests
{
    public class MergeToRequest
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public MergeToSourceType SourceType { get; set; }
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Comment { get; set; }
        public bool CreateShelve { get; set; }
        public bool EnsureNoDstChanges { get; set; }
    }

    public enum MergeToSourceType : byte
    {
        Branch = 0,
        Shelve = 1,
        Label = 2,
        Changeset = 3
    }
}
