using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrunkBot.Messages
{
    class BranchAttributeChangeEvent
    {
        public string Repository { get; set; }
        public string BranchId { get; set; }
        public string BranchFullName { get; set; }
        public string BranchOwner { get; set; }
        public string BranchComment { get; set; }
        public string AttributeName { get; set; }
        public string AttributeValue { get; set; }
    }

    static class ParseEvent
    {
        internal static BranchAttributeChangeEvent Parse(string message)
        {
            string properties = GetPropertiesFromMessage(message);
            return JsonConvert.DeserializeObject<BranchAttributeChangeEvent>(properties);
        }

        static string GetPropertiesFromMessage(string message)
        {
            try
            {
                JObject obj = JObject.Parse(message);
                return obj.Value<object>("properties").ToString();
            }
            catch
            {
                // pending to add log
                return string.Empty;
            }
        }
    }
}
