using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codice.CM.Server.Devops
{
    public interface INotifierPlugService
    {
        Task NotifyMessage(
            string name,
            string text,
            List<string> recipients);
    }
}