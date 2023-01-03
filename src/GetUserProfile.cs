using Newtonsoft.Json.Linq;

using Codice.CM.Server.Devops;
using TrunkBot.Api;

namespace TrunkBot
{
    class GetUserProfile : IGetUserProfile
    {
        internal GetUserProfile(RestApi restApi)
        {
            mRestApi = restApi;
        }
            
        JObject IGetUserProfile.GetUserProfile(string userName)
        {
            return mRestApi.Users.GetUserProfile(userName);
        }

        readonly RestApi mRestApi;
    }
}