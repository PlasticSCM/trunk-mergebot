using System;
using System.Collections.Generic;
using System.Text;

namespace Codice.LogWrapper
{
    public interface ILog
    {
        void Debug(object message);

        void DebugFormat(string format, params object[] args);

        void Info(object message);

        void InfoFormat(string format, params object[] args);

        void Warn(object message);

        void WarnFormat(string format, params object[] args);

        void Error(object message);

        void Error(object message, Exception exception);

        void ErrorFormat(string format, params object[] args);

        void Fatal(object message);

        void FatalFormat(string format, params object[] args);

        bool IsDebugEnabled();

        bool IsInfoEnabled();

        bool IsWarnEnabled();
    }
}