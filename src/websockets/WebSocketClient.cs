using System;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using log4net;
using WebSocketSharp;

namespace TrunkBot.WebSockets
{
    class WebSocketClient
    {
        internal WebSocketClient(
            string serverUrl,
            string name,
            string apikey,
            Action<object> processMessage)
        {
            mName = name;
            mApiKey = apikey;

            mWebSocket = new WebSocket(serverUrl);
            mWebSocket.OnMessage += OnMessage;
            mWebSocket.OnClose += OnClose;
            mWebSocket.OnError += OnError;
            mWebSocket.Log.Output = LogOutput;
            mWebSocket.SslConfiguration.ServerCertificateValidationCallback += CertificateValidation;

            mProcessMessage = processMessage;
        }

        internal void ConnectWithRetries()
        {
            if (mbIsTryingConnection)
                return;

            mbIsTryingConnection = true;
            try
            {
                while (true)
                {
                    if (Connect())
                        return;

                    System.Threading.Thread.Sleep(5000);
                }
            }
            finally
            {
                mbIsTryingConnection = false;
            }
        }

        bool Connect()
        {
            mWebSocket.Connect();
            if (!mWebSocket.IsAlive)
                return false;

            mWebSocket.Send(StartupMessages.BuildLoginMessage(mApiKey));
            mWebSocket.Send(StartupMessages.BuildRegisterTriggerMessage("branchAttributeChanged"));

            mLog.InfoFormat("TrunkBot [{0}] connected!", mName);
            return true;
        }

        void OnClose(object sender, CloseEventArgs closeEventArgs)
        {
            mLog.InfoFormat(
                "OnClose was called! Code [{0}]. Reason [{1}]",
                closeEventArgs.Code, closeEventArgs.Reason);

            ConnectWithRetries();
        }

        void OnMessage(object sender, MessageEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(mProcessMessage), e.Data);
        }

        void OnError(object sender, ErrorEventArgs e)
        {
            mLog.ErrorFormat("WebSocket connection error: {0}", e.Message);
            mLog.DebugFormat(
                "Stack trace:{0}{1}", Environment.NewLine, e.Exception.StackTrace);
        }

        static void LogOutput(LogData arg1, string arg2)
        {
        }

        static bool CertificateValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        readonly WebSocket mWebSocket;

        readonly ILog mLog = LogManager.GetLogger("websocket");
        readonly string mName;
        readonly string mApiKey;
        readonly Action<object> mProcessMessage;

        volatile bool mbIsTryingConnection = false;
    }
}
