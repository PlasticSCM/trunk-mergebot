using Newtonsoft.Json.Linq;

namespace Codice.CM.Server.Devops
{
    public interface IGetUserProfile
    {
        JObject GetUserProfile(string userName);
    }
}