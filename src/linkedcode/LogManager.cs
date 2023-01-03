using System;
using System.Collections.Generic;

namespace Codice.LogWrapper
{
    public class LogManager
    {
        // This class should not be instantiated
        private LogManager()
        {
        }

        public static ILog GetLogger(string name)
        {
            return new NetLogger(name);
        }

        public static ILog GetLogger(Type type)
        {
            return new NetLogger(type);
        }

        public static void SetThreadContextProperty(
            string propertyName, string value)
        {
            NetLogger.SetThreadContextProperty(propertyName, value);
        }

        public static List<string> GetLogFileAppenders()
        {
            return NetLogger.GetLogFileAppenders();
        }

        public static void Shutdown()
        {
            NetLogger.Shutdown();
        }
    }
}