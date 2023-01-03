using System;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using Codice.LogWrapper;
using WebSocketSharp;

namespace TrunkBot.WebSockets
{
    class WebSocketClient
    {
        internal const string BRANCH_ATTRIBUTE_CHANGED_TRIGGER_TYPE = "branchAttributeChanged";
        internal const string CODE_REVIEW_CHANGED_TRIGGER_TYPE = "codeReviewChanged";

        internal WebSocketClient(
            string serverUrl,
            string name,
            string apikey,
            string[] eventNamesToSubscribe,
            Action<object> processMessage)
        {
            mName = name;
            mApiKey = apikey;

            mWebSocket = new WebSocket(serverUrl);
            mWebSocket.OnMessage += OnMessage;
            mWebSocket.OnClose += OnClose;
            mWebSocket.OnError += OnError;
            mWebSocket.Log.Output = LogOutput;

            if (mWebSocket.IsSecure)
                mWebSocket.SslConfiguration.ServerCertificateValidationCallback += CertificateValidation;

            mEventNamesToSubscribe = eventNamesToSubscribe;

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

            foreach (string eventName in mEventNamesToSubscribe)
            {
                mWebSocket.Send(
                    StartupMessages.BuildRegisterTriggerMessage(eventName));
            }

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

        readonly ILog mLog = LogManager.GetLogger("TrunkBot-WebSocketClient");
        readonly string mName;
        readonly string mApiKey;
        readonly string[] mEventNamesToSubscribe;
        readonly Action<object> mProcessMessage;

        volatile bool mbIsTryingConnection = false;
    }
}
