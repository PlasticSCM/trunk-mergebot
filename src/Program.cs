using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using TrunkBot.Api;
using TrunkBot.Configuration;
using TrunkBot.WebSockets;

namespace TrunkBot
{
    class Program
    {
        static int Main(string[] args)
        {
            string botName = null;
            try
            {
                TrunkBotArguments botArgs = new TrunkBotArguments(args);
                bool bValidArgs = botArgs.Parse();

                string basePath = GetBasePath(botArgs.BasePath);
                botName = botArgs.BotName;

                ConfigureLogging(basePath, botName);

                mLog.InfoFormat("TrunkBot [{0}] started. Version [{1}]",
                    botName,
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

                string argsStr = args == null ? string.Empty : string.Join(" ", args);
                mLog.DebugFormat("Args: [{0}]. Are valid args?: [{1}]", argsStr, bValidArgs);

                if (!bValidArgs || botArgs.ShowUsage)
                {
                    PrintUsage();
                    if (botArgs.ShowUsage)
                    {
                        mLog.InfoFormat(
                            "TrunkBot [{0}] is going to finish: " +
                                "user explicitly requested to show usage.",
                            botName);
                        return 0;
                    }

                    mLog.ErrorFormat(
                        "TrunkBot [{0}] is going to finish: " +
                            "invalid arguments found in command line.",
                        botName);
                    return 0;
                }

                string errorMessage = null;
                if (!TrunkBotArgumentsChecker.CheckArguments(
                        botArgs, out errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    mLog.ErrorFormat(
                        "TrunkBot [{0}] is going to finish: error found on argument check",
                        botName);
                    mLog.Error(errorMessage);
                    return 1;
                }

                TrunkBotConfiguration botConfig = TrunkBotConfiguration.
                    BuildFromConfigFile(botArgs.ConfigFilePath);

                errorMessage = null;
                if (!TrunkBotConfigurationChecker.CheckConfiguration(
                        botConfig, out errorMessage))
                {
                    Console.WriteLine(errorMessage);
                    mLog.ErrorFormat(
                        "TrunkBot [{0}] is going to finish: error found on argument check",
                        botName);
                    mLog.Error(errorMessage);
                    return 1;
                }

                ConfigureServicePoint();

                string escapedBotName = GetEscapedBotName(botName);

                Task trunkMergebot = LaunchTrunkMergebot(
                    botArgs.WebSocketUrl,
                    botArgs.RestApiUrl,
                    botConfig,
                    botName,
                    botArgs.ApiKey);

                mLog.InfoFormat(
                    "TrunkBot [{0}] is going to finish: orderly shutdown.",
                    botName);

                trunkMergebot.Wait();
                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                mLog.FatalFormat(
                    "TrunkBot [{0}] is going to finish: uncaught exception " +
                    "thrown during execution. Message: {1}", botName, e.Message);
                mLog.DebugFormat("StackTrace: {0}", e.StackTrace);
                return 1;
            }
        }

        static string GetBasePath(string basePath)
        {
            if (string.IsNullOrEmpty(basePath))
            {
                return Path.GetDirectoryName(
                    System.Reflection.Assembly.GetExecutingAssembly().Location);
            }

            return basePath;
        }

        static async Task LaunchTrunkMergebot(
            string webSocketUrl,
            string restApiUrl,
            TrunkBotConfiguration botConfig,
            string botName,
            string apiKey)
        {
            RestApi restApi = new RestApi(restApiUrl, botConfig.UserApiKey);
            TrunkMergebot trunkBot = new TrunkMergebot(
                new IssueTrackerPlugService(restApi),
                new NotifierPlugService(restApi),
                new ContinuousIntegrationPlugService(restApi),
                new RepositoryOperationsForTrunkbot(restApi),
                new GetUserProfile(restApi),
                new ReportMerge(restApi),
                null,
                botConfig, 
                botName);

            await ((IMergebotService)trunkBot).Initialize();

            Task botTask = ((IMergebotService)trunkBot).Start();

            string[] eventsToSubscribe = GetEventNamesToSuscribe(
                botConfig.Plastic.IsApprovedCodeReviewFilterEnabled,
                botConfig.Plastic.IsBranchAttrFilterEnabled);

            WebSocketTrigger webSocketTrigger = new WebSocketTrigger(trunkBot);
            
            WebSocketClient ws = new WebSocketClient(
                webSocketUrl,
                botName,
                apiKey,
                eventsToSubscribe,
                webSocketTrigger.OnEventReceived);

            ws.ConnectWithRetries();

            await botTask;
        }

        static string[] GetEventNamesToSuscribe(
            bool isApprovedCodeReviewFilterEnabled, 
            bool isBranchAttrFilterEnabled)
        {
            List<string> eventNamesList = new List<string>();
            if (isApprovedCodeReviewFilterEnabled)
                eventNamesList.Add(WebSocketClient.CODE_REVIEW_CHANGED_TRIGGER_TYPE);

            if (isBranchAttrFilterEnabled)
                eventNamesList.Add(WebSocketClient.BRANCH_ATTRIBUTE_CHANGED_TRIGGER_TYPE);

            return eventNamesList.ToArray();
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

        static void ConfigureLogging(string basePath, string botName)
        {
            if (string.IsNullOrEmpty(botName))
                botName = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

            try
            {
                string logOutputPath = Path.GetFullPath(Path.Combine(
                   basePath,
                   "../../../logs",
                   "trunkbot." + botName + ".log.txt"));

                string log4netConfPath = ToolConfig.GetLogConfigFile(basePath);
                log4net.GlobalContext.Properties["LogOutputPath"] = logOutputPath;
                Configurator.Configure(log4netConfPath);
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

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-Main");
    }
}
