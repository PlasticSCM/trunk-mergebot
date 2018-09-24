using Newtonsoft.Json.Linq;

namespace TrunkBot.WebSockets
{
    internal static class StartupMessages
    {
        internal static string BuildRegisterTriggerMessage(params string[] triggers)
        {
            JObject obj = new JObject(
                new JProperty("action", "register"),
                new JProperty("type", "trigger"),
                new JProperty("eventlist", new JArray(triggers)));

            return obj.ToString();
        }

        internal static string BuildLoginMessage(string token)
        {
            JObject obj = new JObject(
                new JProperty("action", "login"),
                new JProperty("key", token));

            return obj.ToString();
        }
    }
}
