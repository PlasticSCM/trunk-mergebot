using System.Collections.Generic;
using System.Threading.Tasks;
using Codice.CM.Server.Devops;
using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;

namespace TrunkBot
{
    public class ContinuousIntegrationPlugService : IContinuousIntegrationPlugService
    {
        internal ContinuousIntegrationPlugService(RestApi restApi)
        {
            mRestApi = restApi;
        }

        async Task<string> IContinuousIntegrationPlugService.LaunchPlan(
            string ciName,
            string planName,
            string objectSpec,
            string comment,
            Dictionary<string, string> properties)
        {
            LaunchPlanRequest request = new LaunchPlanRequest()
            {
                ObjectSpec = objectSpec,
                Comment = string.Format("MergeBot - {0}", comment),
                Properties = properties
            };

            SingleResponse planResponse = await mRestApi.CI.LaunchPlan(
                ciName, planName, request);

            return planResponse.Value;
        }

        async Task<PlanStatus> IContinuousIntegrationPlugService.GetPlanStatus(
            string ciName, string planName, string executionId)
        {
            GetPlanStatusResponse response = await mRestApi.CI.GetPlanStatus(
                ciName, planName, executionId);
            if (response == null)
                return null;
            
            PlanStatus result = new PlanStatus();
            result.Succeeded = response.Succeeded;
            result.IsFinished = response.IsFinished;
            result.Explanation = response.Explanation;
            return result;
        }

        readonly RestApi mRestApi;
    }
}