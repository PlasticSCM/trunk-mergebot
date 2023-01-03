using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codice.CM.Server.Devops
{
    public interface IIssueTrackerPlugService
    {
        bool IsIssueTrackerConnected(string issueTrackerName);

        Task<string> GetIssueUrl(
            string issueTrackerName,
            string projectKey,
            string taskNumber);

        Task<string> GetIssueFieldValue(
            string issueTrackerName,
            string projectKey,
            string taskNumber,
            string fieldName);

        Task SetIssueFieldValue(
            string issueTrackerName,
            string projectKey,
            string taskNumber,
            string fieldName,
            string newValue);

        Task CreateRelease(
            string issueTrackerName,
            string projectKey,
            string releaseName,
            string releaseComment,
            List<string> taskNumbers);

        Task<List<string>> GetReleaseTasks(
            string issueTrackerName,
            string projectKey,
            string releaseName);
    }
}