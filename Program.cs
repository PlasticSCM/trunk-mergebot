using System;
using System.IO;
using System.Net;

using log4net;
using log4net.Config;

using TrunkBot.Configuration;
using TrunkBot.WebSockets;

namespace TrunkBot
{
    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                TrunkBotArguments botArgs = new TrunkBotArguments(args);

                bool bValidArgs = botArgs.Parse();

                ConfigureLogging(botArgs.BotName);

                string argsStr = args == null ? string.Empty : string.Join(" ", args);
                mLog.DebugFormat("Args: [{0}]. Are valid args?: [{1}]", argsStr, bValidArgs);

                if (!bValidArgs || botArgs.ShowUsage)
                {
                    PrintUsage();
                    return 0;
                }

                string errorMessage = null;
                if (!TrunkBotArgumentsChecker.CheckArguments(
                        botArgs, out errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    mLog.Error(errorMessage);
                    return 1;
                }

                TrunkBotConfiguration botConfig = TrunkBotConfiguration.
                    BuidFromConfigFile(botArgs.ConfigFilePath);

                errorMessage = null;
                if (!TrunkBotConfigurationChecker.CheckConfiguration(
                        botConfig, out errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    mLog.Error(errorMessage);
                    return 1;
                }

                ConfigureServicePoint();

                LaunchTrunkMergebot(
                    botArgs.WebSocketUrl,
                    botArgs.RestApiUrl,
                    botConfig,
                    ToolConfig.GetBranchesFile(
                        GetEscapedBotName(botArgs.BotName)),
                    botArgs.BotName,
                    botArgs.ApiKey);

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mLog.ErrorFormat("Error: {0}", e.Message);
                mLog.DebugFormat("StackTrace: {0}", e.StackTrace);
                return 1;
            }
        }

        static void LaunchTrunkMergebot(
            string webSocketUrl,
            string restApiUrl,
            TrunkBotConfiguration botConfig,
            string branchesQueueFilePath,
            string botName,
            string apiKey)
        {
            if(!Directory.Exists(Path.GetDirectoryName(branchesQueueFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(branchesQueueFilePath));

            TrunkMergebot trunkBot = new TrunkMergebot(
                restApiUrl,  botConfig, branchesQueueFilePath, botName);

            trunkBot.LoadBranchesToProcess();

            System.Threading.ThreadPool.QueueUserWorkItem(trunkBot.ProcessBranches);

            WebSocketClient ws = new WebSocketClient(
                webSocketUrl,
                botName,
                apiKey,
                trunkBot.OnAttributeChanged);

            ws.ConnectWithRetries();

            System.Threading.Tasks.Task.Delay(-1).Wait();
        }

        static void ConfigureServicePoint()
        {
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.DefaultConnectionLimit = 500;
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("\ttrunkbot.exe --websocket <WEB_SOCKET_URL>");
            Console.WriteLine("\t             --restapi <REST_API_URL> --apikey <WEB_SOCKET_CONN_KEY>");
            Console.WriteLine("\t             --name <MERGEBOT_NAME> --config <JSON_CONFIG_FILE_PATH>");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("\ttrunkbot.exe --websocket wss://blackmore:7111/plug");
            Console.WriteLine("\t             --restapi https://blackmore:7178 --apikey x2fjk28fda");
            Console.WriteLine("\t             --name trunk-dev-bot --config trunk-dev-bot.conf");
            Console.WriteLine();
        }

        static void ConfigureLogging(string botName)
        {
            if (string.IsNullOrEmpty(botName))
                botName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

            try
            {
                string log4netpath = ToolConfig.GetLogConfigFile();
                log4net.GlobalContext.Properties["Name"] = botName;
                XmlConfigurator.Configure(new FileInfo(log4netpath));
            }
            catch
            {
                //it failed configuring the logging info; nothing to do.
            }
        }

        static string GetEscapedBotName(string botName)
        {
            char[] forbiddenChars = new char[] {
                '/', '\\', '<', '>', ':', '"', '|', '?', '*', ' ' };

            string cleanName = botName;
            if (botName.IndexOfAny(forbiddenChars) != -1)
            {
                foreach (char character in forbiddenChars)
                    cleanName = cleanName.Replace(character, '-');
            }

            return cleanName;
        }

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
