using System.Text.RegularExpressions;
using Codice.CM.Server.Devops;

namespace TrunkBot.Labeling
{
    public static partial class AutomaticLabeler
    {
        public static class NewLabelNameGenerator
        {
            public static string GetNewLabelName(string plasticFindPattern, Label lastMatchingLabel)
            {
                if (plasticFindPattern.IndexOf(Tokens.ANY_WILDCARD) < 0)
                    return plasticFindPattern;

                if (lastMatchingLabel == null)
                    return plasticFindPattern.Replace(Tokens.ANY_WILDCARD, "0");

                string regex = plasticFindPattern.Replace(Tokens.ANY_WILDCARD, @"(\d+)");

                if (!Regex.IsMatch(lastMatchingLabel.Name, regex))
                    return string.Empty; //error

                return Regex.Replace(
                    lastMatchingLabel.Name, 
                    regex, 
                    new MatchEvaluator(AutoIncrementNumbers));
            }

            static string AutoIncrementNumbers(Match match)
            {
                string result = match.Value;

                int parsedNum;
                for (int i = match.Groups.Count - 1; i > 0; i--)
                {
                    if (!int.TryParse(match.Groups[i].Value, out parsedNum))
                        continue;

                    result = result.Remove(match.Groups[i].Index, match.Groups[i].Length);
                    result = result.Insert(match.Groups[i].Index, (++parsedNum).ToString());
                }

                return result;
            }
        }
    }
}

