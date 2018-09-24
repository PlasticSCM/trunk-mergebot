using System.Collections.Generic;

namespace TrunkBot.Api.Requests
{
    public class NotifyMessageRequest
    {
        public string Message { get; set; }
        public List<string> Recipients { get; set; }
    }
}
