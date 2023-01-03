using System.Collections.Generic;
using System.IO;

namespace Codice.LogWrapper
{
    public class Configurator
    {
        // This class should not be instantiated
        private Configurator()
        {
        }

        public static void Configure(string path)
        {
            if (!File.Exists(path))
                return;

            NetLogger.Configure(path);
        }

        public static void ConfigureAndWatch(string path)
        {
            if (!File.Exists(path))
                return;

            NetLogger.ConfigureAndWatch(path);
        }

        public static void ConfigureAndWatchWithCustomProperties(
            string path,
            Dictionary<string, string> customLogProperties)
        {
            if (!File.Exists(path))
                return;

            NetLogger.ConfigureAndWatchWithCustomProperties(path, customLogProperties);
        }
    }
}