using Newtonsoft.Json.Linq;

namespace TrunkBot
{
    internal static class ParseUserProfile
    {
        internal static string GetFieldFromProfile(
            JObject userProfile, string[] profileFieldsPath)
        {
            if (profileFieldsPath.Length == 0)
                return null;

            if (userProfile == null)
                return null;

            JToken currentField = userProfile;
            foreach (string field in profileFieldsPath)
            {
                currentField = currentField[field];

                if (currentField == null)
                    return null;
            }

            if (currentField.Type != JTokenType.String)
                return null;

            return currentField.Value<string>();
        }
    }
}
