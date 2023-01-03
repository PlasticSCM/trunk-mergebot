using Codice.CM.Server.Devops;
using TrunkBot.Api;
using TrunkBot.Api.Requests;

namespace TrunkBot
{
    internal class ReportMerge : IReportMerge
    {
        internal ReportMerge(RestApi restApi)
        {
            mRestApi = restApi;
        }
            
        void IReportMerge.Report(string mergebotName, MergeReport mergeReport)
        {
            mRestApi.MergeReports.ReportMerge(mergebotName, mergeReport).Wait();
        }
            
        readonly RestApi mRestApi;
    }
}