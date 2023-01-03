namespace Codice.CM.Server.Devops
{
    public interface IReportMerge
    {
        void Report(string mergebotName, MergeReport mergeReport);
    }
}