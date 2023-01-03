using System;
using System.Collections.Generic;
using System.IO;

using log4net;
using log4net.Config;
using log4net.Repository;
using log4net.Appender;

namespace Codice.LogWrapper
{
    public class NetLogger : ILog
    {
        internal NetLogger(string name)
        {
            mLogger = log4net.LogManager.GetLogger(mDefaultLoggerRepository.Name, name);
        }

        internal NetLogger(Type type)
        {
            mLogger = log4net.LogManager.GetLogger(mDefaultLoggerRepository.Name, type);
        }

        internal static void Configure(string path)
        {
            log4net.Config.XmlConfigurator.Configure(mDefaultLoggerRepository, new FileInfo(path));
        }

        internal static void ConfigureAndWatch(string path)
        {
            log4net.Config.XmlConfigurator.ConfigureAndWatch(mDefaultLoggerRepository, new FileInfo(path));
        }

        internal static void ConfigureAndWatchWithCustomProperties(
            string path, 
            Dictionary<string, string> customLogProperties)
        {
            SetCustomProperties(customLogProperties);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(mDefaultLoggerRepository, new FileInfo(path));
        }

        internal static void Shutdown()
        {
            log4net.LogManager.ShutdownRepository(mDefaultLoggerRepository.Name);
        }

        public void Debug(object message)
        {
            mLogger.Debug(message);
        }

        public void DebugFormat(string format, params object[] args)
        {
            mLogger.DebugFormat(format, args);
        }

        public void Info(object message)
        {
            mLogger.Info(message);
        }

        public void InfoFormat(string format, params object[] args)
        {
            mLogger.InfoFormat(format, args);
        }

        public void Warn(object message)
        {
            mLogger.Warn(message);
        }

        public void WarnFormat(string format, params object[] args)
        {
            mLogger.WarnFormat(format, args);
        }

        public void Error(object message)
        {
            mLogger.Error(message);
        }

        public void Error(object message, Exception exception)
        {
            mLogger.Error(message, exception);
        }

        public void ErrorFormat(string format, params object[] args)
        {
            mLogger.ErrorFormat(format, args);
        }

        public void Fatal(object message)
        {
            mLogger.Fatal(message);
        }

        public void FatalFormat(string format, params object[] args)
        {
            mLogger.FatalFormat(format, args);
        }

        public bool IsDebugEnabled()
        {
            return mLogger.IsDebugEnabled;
        }

        public bool IsInfoEnabled()
        {
            return mLogger.IsInfoEnabled;
        }

        public bool IsWarnEnabled()
        {
            return mLogger.IsWarnEnabled;
        }

        public static void SetThreadContextProperty(string propertyName, string value)
        {
            ThreadContext.Properties[propertyName] = value;
        }

        public static List<T> GetAppendersByType<T>()
        {
            List<T> result = new List<T>();
            foreach (ILoggerRepository rep in log4net.LogManager.GetAllRepositories())
            {
                foreach (IAppender appender in rep.GetAppenders())
                {
                    if (!(appender is T))
                        continue;

                    result.Add((T)appender);
                }
            }

            return result;
        }

        internal static List<string> GetLogFileAppenders()
        {
            List<string> result = new List<string>();

            foreach (FileAppender fileAppender in GetAppendersByType<FileAppender>())
                    result.Add(fileAppender.File);

            return result;
        }

        static void SetCustomProperties(Dictionary<string, string> customLogProperties)
        {
            if (customLogProperties == null || customLogProperties.Count == 0)
                return;

            string keyValue = null;
            foreach (string key in customLogProperties.Keys)
            {
                if (string.IsNullOrEmpty(key))
                    continue;

                keyValue = customLogProperties[key];
                if (string.IsNullOrEmpty(keyValue))
                    continue;

                GlobalContext.Properties[key] = keyValue;
            }
        }

        static NetLogger()
        {
            //DON'T CHANGE THE REPOSITORY NAME.
            //This name is needed to support use log4net and the logwrapper on the same program.
            string defaultRepName = "log4net-default-repository";
            try
            {
                //If the repository already exist, CreateRepository will fail
                mDefaultLoggerRepository = log4net.LogManager.CreateRepository(defaultRepName);
            }
            catch
            {
                //If the repository doesn't exist, GetRepository will fail
                mDefaultLoggerRepository = log4net.LogManager.GetRepository(defaultRepName);

                if (mDefaultLoggerRepository != null)
                    return;

                throw;
            }
        }

        static readonly log4net.Repository.ILoggerRepository mDefaultLoggerRepository;

        readonly log4net.ILog mLogger;
    }
}