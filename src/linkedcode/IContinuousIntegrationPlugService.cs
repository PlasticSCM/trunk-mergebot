using System.Collections.Generic;
using System.Threading.Tasks;

namespace Codice.CM.Server.Devops
{
    public class PlanStatus
    {
        public bool IsFinished;
        public bool Succeeded;
        public string Explanation;

        // this is something only used by jenkins (at this moment)
        // because is the only ci plug that:
        // * the plan launch returns a queue id
        // * this queue id is used to translate to the build url once its available
        //   after the build is started
        // * the queue id becomes useless some minutes after the build is started
        // * all the build status needs to be queried using the build url.
        public string TranslatedBuildId;

        public static PlanStatus Cancelled = new PlanStatus()
        {
            IsFinished = false,
            Succeeded = false,
            Explanation = "The operation was canceled"
        };
    }

    public interface IContinuousIntegrationPlugService
    {
        Task<string> LaunchPlan(
            string ciName,
            string planName,
            string objectSpec,
            string comment,
            Dictionary<string, string> properties);

        Task<PlanStatus> GetPlanStatus(
            string ciName,
            string planName,
            string executionId);
    }
}