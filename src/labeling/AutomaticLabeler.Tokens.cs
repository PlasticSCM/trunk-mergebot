namespace TrunkBot.Labeling
{
    public static partial class AutomaticLabeler
    {
        public static class Tokens
        {
            internal const string START_VARIABLE = "${";
            internal const string END_VARIABLE = "}";
            internal const string COMMA = ",";
            internal const string ANY_WILDCARD = "%";
            internal const string AUTO_INCREMENT_TAG = "AUTO_INCREMENT_NUMBER";
            internal const string DATE_FORMATTED_TAG = "BUILD_DATE_FORMATTED";
            internal const string AUTO_INCREMENT_EXISTING_PATTERN = "_" + ANY_WILDCARD;
        }
    }
}

