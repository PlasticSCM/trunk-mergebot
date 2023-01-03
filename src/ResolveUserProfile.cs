using System;
using System.Collections.Generic;
using Codice.CM.Server.Devops;
using Codice.LogWrapper;
using Newtonsoft.Json.Linq;

namespace TrunkBot
{
    internal static class ResolveUserProfile
    {
        internal static List<string> ResolveFieldForUsers(
            IGetUserProfile userProfile,
            List<string> users,
            string profileFieldQualifiedName)
        {
            if (string.IsNullOrEmpty(profileFieldQualifiedName))
                return null;

            string[] profileFieldsPath = profileFieldQualifiedName.Split(
                new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> result = new List<string>();

            foreach (string user in users)
            {
                string solvedUser;

                if (!TryResolveUserProfileValue(
                    userProfile, user, profileFieldsPath, out solvedUser))
                {
                    Add(user, result);
                    continue;
                }

                Add(solvedUser, result);
            }

            return result;
        }

        static void Add(string resolvedValue, List<string> result)
        {
            if (result.Contains(resolvedValue))
                return;

            result.Add(resolvedValue);
        }

        static bool TryResolveUserProfileValue(
            IGetUserProfile userProfile, 
            string user, 
            string[] profileFieldsPath, 
            out string solvedUser)
        {
            solvedUser = null;

            JObject userProfileResponse = GetUserProfile(userProfile, user);

            if (userProfileResponse == null)
                return false;

            solvedUser = ParseUserProfile.GetFieldFromProfile(
                userProfileResponse, profileFieldsPath);

            return !string.IsNullOrEmpty(solvedUser);
        }

        static JObject GetUserProfile(IGetUserProfile userProfile, string user)
        {
            try
            {
                return userProfile.GetUserProfile(user);
            }
            catch (Exception e)
            {
                mLog.WarnFormat(
                    "Unable to resolve user's profile for username {0} : {1}",
                    user, e.Message);

                return null;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-Notifier");
    }
}
