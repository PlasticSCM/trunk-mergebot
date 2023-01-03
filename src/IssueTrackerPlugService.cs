using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codice.CM.Server.Devops;
using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;

namespace TrunkBot
{
    class IssueTrackerPlugService : IIssueTrackerPlugService
    {
        internal IssueTrackerPlugService(RestApi restApi)
        {
            mRestApi = restApi;
        }
        
        bool IIssueTrackerPlugService.IsIssueTrackerConnected(string issueTrackerName)
        {
            SingleResponse response = mRestApi.Issues.IsConnected(issueTrackerName).GetAwaiter().GetResult();
            return GetBoolValue(response.Value, false);
        }

        async Task<string> IIssueTrackerPlugService.GetIssueUrl(
            string issueTrackerName,
            string projectKey,
            string taskNumber)
        {
            SingleResponse response = await mRestApi.Issues.GetIssueUrl(
                issueTrackerName,
                projectKey, 
                taskNumber);

            return response.Value;
        }

        async Task<string> IIssueTrackerPlugService.GetIssueFieldValue(
            string issueTrackerName,
            string projectKey,
            string taskNumber,
            string fieldName)
        {
            SingleResponse response = await mRestApi.Issues.GetIssueField(issueTrackerName,
                projectKey, taskNumber, fieldName);
            return response.Value;
        }

        async Task IIssueTrackerPlugService.SetIssueFieldValue(
            string issueTrackerName,
            string projectKey,
            string taskNumber,
            string fieldName,
            string newValue)
        {
            SetIssueFieldRequest request = new SetIssueFieldRequest()
            {
                NewValue = newValue
            };

            await mRestApi.Issues.SetIssueField(issueTrackerName,
                projectKey, taskNumber, fieldName, request);
        }

        Task IIssueTrackerPlugService.CreateRelease(string issueTrackerName, string projectKey, string releaseName,
            string releaseComment, List<string> taskNumbers)
        {
            throw new System.NotImplementedException();
        }

        Task<List<string>> IIssueTrackerPlugService.GetReleaseTasks(string issueTrackerName, string projectKey, string releaseName)
        {
            throw new System.NotImplementedException();
        }

        static bool GetBoolValue(string value, bool defaultValue)
        {
            bool flag;
            return Boolean.TryParse(value, out flag) ? flag : defaultValue;
        }    

        readonly RestApi mRestApi;
    }
}