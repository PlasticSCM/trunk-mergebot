using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

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
            string botName = null;
            try
            {
                TrunkBotArguments botArgs = new TrunkBotArguments(args);

                bool bValidArgs = botArgs.Parse();
                botName = botArgs.BotName;

                ConfigureLogging(botName);

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
                    BuidFromConfigFile(botArgs.ConfigFilePath);

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

                LaunchTrunkMergebot(
                    botArgs.WebSocketUrl,
                    botArgs.RestApiUrl,
                    botConfig,
                    ToolConfig.GetBranchesFile(escapedBotName),
                    ToolConfig.GetCodeReviewsFile(escapedBotName),
                    botName,
                    botArgs.ApiKey);

                mLog.InfoFormat(
                    "TrunkBot [{0}] is going to finish: orderly shutdown.",
                    botName);

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

        static void LaunchTrunkMergebot(
            string webSocketUrl,
            string restApiUrl,
            TrunkBotConfiguration botConfig,
            string branchesQueueFilePath,
            string codeReviewsTrackedFilePath,
            string botName,
            string apiKey)
        {
            if (!Directory.Exists(Path.GetDirectoryName(branchesQueueFilePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(branchesQueueFilePath));

            TrunkMergebot trunkBot = new TrunkMergebot(
                restApiUrl,
                botConfig, 
                branchesQueueFilePath,
                codeReviewsTrackedFilePath,
                botName);

            try
            {
                trunkBot.EnsurePlasticStatusAttributeExists();
            }
            catch (Exception e)
            {
                mLog.FatalFormat(
                    "TrunkBot [{0}] is going to finish because it wasn't able " +
                    "to configure the required plastic status attribute [{1}] for its proper working. " +
                    "Reason: {2}", botName, botConfig.Plastic.StatusAttribute.Name, e.Message);

                mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }

            try
            { 
                trunkBot.LoadBranchesToProcess();
            }
            catch (Exception e)
            {
                mLog.FatalFormat(
                    "TrunkBot [{0}] is going to finish because it couldn't load " +
                    "the branches to process on startup. Reason: {1}", botName, e.Message);
                mLog.DebugFormat("Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                throw;
            }

            ThreadPool.QueueUserWorkItem(trunkBot.ProcessBranches);

            string[] eventsToSubscribe = GetEventNamesToSuscribe(
                botConfig.Plastic.IsApprovedCodeReviewFilterEnabled,
                botConfig.Plastic.IsBranchAttrFilterEnabled);

            WebSocketClient ws = new WebSocketClient(
                webSocketUrl,
                botName,
                apiKey,
                eventsToSubscribe,
                trunkBot.OnEventReceived);

            ws.ConnectWithRetries();

            Task.Delay(-1).Wait();
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
