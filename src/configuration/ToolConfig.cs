using System.IO;
using System.Reflection;

namespace TrunkBot.Configuration
{
    internal static class ToolConfig
    {
        internal static string GetLogConfigFile(string basePath)
        {
            return Path.Combine(basePath, LOG_CONFIG_FILE);
        }

        const string LOG_CONFIG_FILE = "trunkbot.log.conf";
    }
}
