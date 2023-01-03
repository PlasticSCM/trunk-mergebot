using log4net.Core;
using log4net;

namespace Codice.LogWrapper
{
    public static class ContextProperties
    {
        public static ContextPropertyInfo[] Get()
        {
            string[] propertyNames = ThreadContext.Properties.GetKeys();

            if (propertyNames == null)
                return null;

            ContextPropertyInfo[] result = new ContextPropertyInfo[propertyNames.Length];

            for (int i = 0; i < propertyNames.Length; i++)
            {
                string propertyName = propertyNames[i];
                result[i] = new ContextPropertyInfo(
                    propertyName, ThreadContext.Properties[propertyName]);
            }

            return result;
        }

        public static void Set(ContextPropertyInfo[] properties)
        {
            ThreadContext.Properties.Clear();

            if (properties == null)
                return;

            foreach (ContextPropertyInfo property in properties)
                ThreadContext.Properties[property.Name] = property.Value;
        }
    }

    public class ContextPropertyInfo
    {
        public readonly string Name;
        public readonly object Value;

        public ContextPropertyInfo(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }

    public class LoggingEventInfo
    {
        public readonly LoggingEvent LoggingEvent;
        public readonly ContextPropertyInfo[] ContextProperties;

        LoggingEventInfo(LoggingEvent loggingEvent, ContextPropertyInfo[] contextProperties)
        {
            LoggingEvent = loggingEvent;
            ContextProperties = contextProperties;
        }

        public static LoggingEventInfo Build(
            LoggingEvent loggingEvent, ContextPropertyInfo[] contextProperties)
        {
            SaveOriginalThreadName(loggingEvent);
            return new LoggingEventInfo(loggingEvent, contextProperties);
        }

        static void SaveOriginalThreadName(LoggingEvent loggingEvent)
        {
            if (loggingEvent == null)
                return;

            //HACK: when the ThreadName is read the value of the property is cached.
            //As a result, when the background thread access to it to create the log
            //message the original ThreadName, that is cached, is used.
            string aux = loggingEvent.ThreadName;
        }
    }
}
