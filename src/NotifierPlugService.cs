using System.Collections.Generic;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;
using TrunkBot.Api;
using TrunkBot.Api.Requests;

namespace TrunkBot
{
    class NotifierPlugService : INotifierPlugService
    {
        internal NotifierPlugService(RestApi restApi)
        {
            mRestApi = restApi;
        }
            
        async Task INotifierPlugService.NotifyMessage(string name, string text, List<string> recipients)
        {
            NotifyMessageRequest request = new NotifyMessageRequest()
            {
                Message = text,
                Recipients = recipients
            };

            await mRestApi.Notify.NotifyMessage(name, request);
        }

        readonly RestApi mRestApi;
    }
}