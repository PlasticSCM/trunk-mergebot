using System;
using System.Collections.Generic;

using Newtonsoft.Json.Linq;

using TrunkBot.Api;

namespace TrunkBot
{
    internal static class ResolveUserProfile
    {
        internal static string ResolveField(
            RestApi restApi,
            string user,
            string profileFieldQualifiedName)
        {
            if (string.IsNullOrEmpty(profileFieldQualifiedName))
                return null;

            string[] profileFieldsPath = profileFieldQualifiedName.Split(
                new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            return ParseUserProfile.GetFieldFromProfile(
                TrunkMergebotApi.Users.GetUserProfile(restApi, user), profileFieldsPath);
        }
    }
}
