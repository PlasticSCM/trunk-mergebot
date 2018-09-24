namespace TrunkBot
{
    internal class TrunkBotArguments
    {
        internal TrunkBotArguments(string[] args)
        {
            mArgs = args;
        }

        internal bool Parse()
        {
            return LoadArguments(mArgs);
        }

        internal bool ShowUsage
        {
            get { return mShowUsage; }
        }

        internal string WebSocketUrl
        {
            get { return mWebSocketUrl; }
        }

        internal string RestApiUrl
        {
            get { return mRestApiUrl; }
        }

        internal string BotName
        {
            get { return mBotName; }
        }

        internal string ApiKey
        {
            get { return mApiKey; }
        }

        internal string ConfigFilePath
        {
            get { return mConfigFilePath; }
        }

        bool LoadArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                mShowUsage = true;
                return false;
            }

            bool bValidArgs = true;
            for (int i = 0; i < args.Length; i++)
            {
                if (!bValidArgs)
                    return false;

                if (args[i] == null)
                {
                    continue;
                }

                if (IsUsageArgument(args[i]))
                {
                    mShowUsage = true;
                    return true;
                }

                if (args[i] == WEB_SOCKET_URL_ARG)
                {
                    bValidArgs = ReadArgumentValue(args, ++i, out mWebSocketUrl);
                    continue;
                }

                if (args[i] == API_URL_ARG)
                {
                    bValidArgs = ReadArgumentValue(args, ++i, out mRestApiUrl);
                    continue;
                }

                if (args[i] == BOT_NAME_ARG)
                {
                    bValidArgs = ReadArgumentValue(args, ++i, out mBotName);
                    continue;
                }

                if (args[i] == API_KEY_ARG)
                {
                    bValidArgs = ReadArgumentValue(args, ++i, out mApiKey);
                    continue;
                }

                if (args[i] == CONFIG_FILE_ARG)
                {
                    bValidArgs = ReadArgumentValue(args, ++i, out mConfigFilePath);
                    continue;
                }
            }

            return bValidArgs;
        }

        static bool ReadArgumentValue(string[] args, int argIndex, out string value)
        {
            value = string.Empty;
            if (argIndex >= args.Length)
                return false;

            value = args[argIndex].Trim();

            foreach (string validArgName in VALID_ARGS_NAMES)
                if (value.Equals(validArgName))
                    return false;

            return true;
        }

        static bool IsUsageArgument(string argument)
        {
            foreach (string validHelpArg in VALID_HELP_ARGS)
                if (argument == validHelpArg)
                    return true;

            return false;
        }

        string[] mArgs;

        bool mShowUsage = false;
        string mWebSocketUrl;
        string mRestApiUrl;
        string mBotName;
        string mApiKey;
        string mConfigFilePath;

        static string[] VALID_HELP_ARGS = new string[] {
            "--help", "-h", "--?", "-?" };

        static string[] VALID_ARGS_NAMES = new string[] {
            WEB_SOCKET_URL_ARG,
            API_URL_ARG,
            BOT_NAME_ARG,
            API_KEY_ARG,
            CONFIG_FILE_ARG };

        const string WEB_SOCKET_URL_ARG = "--websocket";
        const string API_URL_ARG = "--restapi";
        const string BOT_NAME_ARG = "--name";
        const string API_KEY_ARG = "--apikey";
        const string CONFIG_FILE_ARG = "--config";
    }
}
