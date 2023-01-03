using System.Threading;
using System.Threading.Tasks;

namespace Codice.CM.Server.Devops
{
    public interface IMergebotService 
    {
        Task Initialize();
        Task Start();
        void Stop();
    }

    public interface INotifyMergebotTriggerActions
    {
        Task NotifyNewChangesets(string repoName, string branchName);

        Task NotifyBranchAttributeChanged(
            string repoName,
            int branchId,
            string branchName,
            string attributeName,
            string attributeValue,
            string branchOwner,
            string branchComment);

        Task NotifyCodeReviewStatusChanged(
            string repoName, 
            int branchId, 
            string branchName, 
            string branchOwner, 
            string branchComment, 
            int reviewId,
            string reviewTitle, 
            string reviewStatus);
    }
}