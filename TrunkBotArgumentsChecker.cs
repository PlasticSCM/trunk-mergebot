using System;
using System.IO;

using Newtonsoft.Json.Linq;

namespace TrunkBot
{
    internal static class TrunkBotArgumentsChecker
    {
        internal static bool CheckArguments(
            TrunkBotArguments botArgs,
            out string errorMessage)
        {
            if (!CheckValidArguments(botArgs, out errorMessage))
            {
                errorMessage = string.Format(
                    "trunkbot can't start without specifying the following arguments:\n{0}",
                    errorMessage);
                return false;
            }

            if (!File.Exists(botArgs.ConfigFilePath))
            {
                errorMessage = string.Format(
                    "trunkbot can't start without the JSON config file [{0}]",
                    botArgs.ConfigFilePath);
                return false;
            }

            if (!TryParseConfigFile(botArgs.ConfigFilePath, out errorMessage))
            {
                errorMessage = string.Format(
                    "trunkbot can't start without specifying a valid JSON config file [{0}]:\n{1}",
                    botArgs.ConfigFilePath, errorMessage);
                return false;
            }

            return true;
        }

        static bool CheckValidArguments(
            TrunkBotArguments botArgs,
            out string errorMessage)
        {
            errorMessage = string.Empty;
           
            if (string.IsNullOrEmpty(botArgs.WebSocketUrl))
                errorMessage += BuildArgumentError(
                    "Plastic web socket url endpoint", "--websocket wss://blackmore:7111/plug");

            if (string.IsNullOrEmpty(botArgs.RestApiUrl))
                errorMessage += BuildArgumentError(
                    "Plastic REST API url", "--restapi https://blackmore:7178");

            if (string.IsNullOrEmpty(botArgs.BotName))
                errorMessage += BuildArgumentError(
                    "Name for this bot", "--name trunk-dev-bot");

            if (string.IsNullOrEmpty(botArgs.ApiKey))
                errorMessage += BuildArgumentError(
                    "Connection API key", "--apikey x2fjk28fda");

            if (string.IsNullOrEmpty(botArgs.ConfigFilePath))
                errorMessage += BuildArgumentError(
                    "JSON config file", "--config tunk-dev-bot-config.conf");

            return string.IsNullOrEmpty(errorMessage);
        }

        static bool TryParseConfigFile(
            string configFilePath, out string errorMessage)
        {
            errorMessage = null;

            try
            {
                string fileContent = File.ReadAllText(configFilePath);

                JObject obj = JObject.Parse(fileContent);

                if (obj != null)
                    return true;

                errorMessage = "The JSON content is empty!";
                return false;
            }
            catch (Exception e)
            {
                errorMessage = e.Message;
                return false;
            }
        }

        static string BuildArgumentError(
            string fieldName, string example)
        {
            return string.Format("* {0}. Example: \"{1}\"\n",
                fieldName, example);
        }
    }
}
