using System;

namespace TrunkBot
{
    internal static class RepositoryNameComparer
    {
        internal static bool IsSameName(string repName1, string repName2)
        {
            return repName1.Equals(repName2, StringComparison.InvariantCulture);
        }
    }
}
