using System;

namespace TrunkBot.Labeling
{
    public static partial class AutomaticLabeler
    {
        public static class PatternTranslator
        {
            public static string ToFindPattern(string labelPattern, DateTime now)
            {
                string plasticFindPattern = labelPattern;

                while (plasticFindPattern.IndexOf(Tokens.START_VARIABLE) >= 0)
                {
                    int startIndex = plasticFindPattern.IndexOf(Tokens.START_VARIABLE);
                    int endIndex = plasticFindPattern.IndexOf(Tokens.END_VARIABLE, startIndex);

                    if (endIndex < startIndex)
                        return string.Empty;

                    string variable = plasticFindPattern.Substring(
                        startIndex, endIndex + Tokens.END_VARIABLE.Length - startIndex);

                    if (HasAnyVariableToken(variable))
                        return string.Empty;

                    plasticFindPattern = plasticFindPattern.Remove(
                        startIndex, endIndex + Tokens.END_VARIABLE.Length - startIndex);

                    plasticFindPattern = plasticFindPattern.Insert(startIndex, ReplaceVariable(variable, now));
                }

                return plasticFindPattern;
            }

            static bool HasAnyVariableToken(string variable)
            {
                variable = variable.Substring(
                    Tokens.START_VARIABLE.Length, 
                    variable.Length - Tokens.START_VARIABLE.Length - Tokens.END_VARIABLE.Length);

                if (variable.IndexOf(Tokens.START_VARIABLE) >= 0 ||
                    variable.IndexOf(Tokens.END_VARIABLE) >= 0)
                {
                    return true;
                }

                return false;
            }

            static string ReplaceVariable(string variable, DateTime now)
            {
                string value = variable.
                    Replace(Tokens.START_VARIABLE, string.Empty).
                    Replace(Tokens.END_VARIABLE, string.Empty).
                    Replace(Tokens.COMMA, string.Empty).
                    Trim();

                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                if (value.StartsWith(Tokens.AUTO_INCREMENT_TAG))
                    return Tokens.ANY_WILDCARD;

                if (!value.StartsWith(Tokens.DATE_FORMATTED_TAG))
                    return string.Empty;

                value = value.Replace(Tokens.DATE_FORMATTED_TAG, string.Empty).Trim();

                if (string.IsNullOrEmpty(value))
                    return string.Empty;

                return now.ToString(value);
            }            
        }
    }
}

