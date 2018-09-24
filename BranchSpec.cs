namespace TrunkBot
{
    internal static class BranchSpec
    {
        internal static string GetName(string fullBranchName)
        {
            int separatorIndex = fullBranchName.LastIndexOf('/');

            if (separatorIndex == -1)
                return fullBranchName;

            return fullBranchName.Substring(separatorIndex + 1);
        }
    }
}
