using System;
using System.Collections.Generic;
using System.Linq;

namespace TrunkBot
{
    public class AttributeComment
    {
        public static string Build(string[] values, string botName)
        {
            string signature = "Attribute automatically created by trunk-bot: " + botName;

            IEnumerable<string> nonEmptyDistinctAttributeValues =
                GetNonEmptyDistinctAttributeValues(values ?? new string[0]);

            if (nonEmptyDistinctAttributeValues.Count() == 0)
                return signature;

            return string.Concat(
                signature,
                Environment.NewLine,
                "default: ",
                string.Join(", ", nonEmptyDistinctAttributeValues));
        }

        static IEnumerable<string> GetNonEmptyDistinctAttributeValues(string[] values)
        {
            return values
                .Select(value => ProtectValue(value))
                .Where(value => value != string.Empty)
                .Distinct();
        }

        static string ProtectValue(string value)
        {
            if (value == null || value.Trim() == string.Empty)
                return string.Empty;

            if (value.Contains(' ') || value.Contains(','))
                return string.Concat("\"", value, "\"");

            return value;
        }
    }
}
