using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TrunkBot.Api.Requests
{
    public class ChangeAttributeRequest
    {
        public string TargetName { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public AttributeTargetType TargetType { get; set; }
        public string Value { get; set; }
    }

    public enum AttributeTargetType : byte
    {
        Branch = 0,
        Label = 1,
        Changeset = 2
    }
}
