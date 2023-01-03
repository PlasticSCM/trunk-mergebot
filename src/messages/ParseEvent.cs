using Codice.LogWrapper;

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

    class CodeReviewChangeEvent
    {
        public string Repository { get; set; }
        public string BranchId { get; set; }
        public string BranchFullName { get; set; }
        public string BranchOwner { get; set; }
        public string BranchComment { get; set; }
        public string CodeReviewId { get; set; }
        public string CodeReviewTitle { get; set; }
        public string CodeReviewStatus { get; set; }
    }

    static class ParseEvent
    {
        internal static T Parse<T>(string message)
        {
            string properties = GetPropertiesFromMessage(message);
            return JsonConvert.DeserializeObject<T>(properties);
        }

        static string GetPropertiesFromMessage(string message)
        {
            try
            {
                JObject obj = JObject.Parse(message);
                return obj.Value<object>("properties").ToString();
            }
            catch (System.Exception e)
            {
                mLog.WarnFormat(
                    "Unable to parse incoming message:[{0}]. {1}", message, e.Message);

                return string.Empty;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-ParseEvent");
    }

    
}
