using System.IO;
using System.Reflection;

namespace TrunkBot.Configuration
{
    internal static class ToolConfig
    {
        internal static string GetLogConfigFile()
        {
            return Path.Combine(GetExecutingAssemblyDirectory(), LOG_CONFIG_FILE);
        }

        internal static string GetBranchesFile(string botName)
        {
            string branchesFileName = string.Format(BRANCHES_FILE, botName);

            return GetConfigFilePath(branchesFileName);
        }

        internal static string GetCodeReviewsFile(string botName)
        {
            string codeReviewsFileName = string.Format(CODE_REVIEWS_FILE, botName);

            return GetConfigFilePath(codeReviewsFileName);
        }

        static string GetConfigFilePath(string configfile)
        {
            return Path.Combine(GetConfigDirectory(), configfile);
        }

        static string GetConfigDirectory()
        {
            return Path.Combine(
                GetExecutingAssemblyDirectory(), CONFIG_FOLDER_NAME);
        }

        static string GetExecutingAssemblyDirectory()
        {
            return Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location);
        }

        const string BRANCHES_FILE = "branches.{0}.txt";
        const string CODE_REVIEWS_FILE = "reviews.{0}.txt";
        const string LOG_CONFIG_FILE = "trunkbot.log.conf";

        const string CONFIG_FOLDER_NAME = "config";
    }
}
