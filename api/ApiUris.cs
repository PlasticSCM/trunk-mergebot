using System;
using System.Net;

namespace TrunkBot.Api
{
    internal static class ApiUris
    {
        internal static Uri GetFullUri(Uri baseUri, string partialUri)
        {
            return new Uri(baseUri, partialUri);
        }

        internal static Uri GetFullUri(Uri baseUri, string partialUri, params string[] args)
        {
            string[] requestParams = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                requestParams[i] = WebUtility.UrlEncode(args[i]);

            string endpoint = string.Format(partialUri, requestParams);
            return new Uri(baseUri, endpoint);
        }
    }

    internal static class ApiEndpoints
    {
        internal const string GetBranch = "/api/v1/repos/{0}/branches/{1}";
        internal const string GetChangeset = "/api/v1/repos/{0}/changesets/{1}";
        internal const string GetAttribute = "/api/v1/repos/{0}/attributes/{1}/{2}/{3}";
        internal const string ChangeAttribute = "/api/v1/repos/{0}/attributes/{1}";
        internal const string MergeTo = "/api/v1/repos/{0}/mergeto";
        internal const string DeleteShelve = "/api/v1/repos/{0}/shelve/{1}";
        internal const string Find = "/api/v1/repos/{0}/find?query={1}&queryDateFormat={2}&fields={3}";

        internal static class Users
        {
            internal const string GetUserProfile = "/api/v1/users/{0}/profile";
        }

        internal static class MergeReports
        {
            internal const string ReportMerge = "/api/v1/mergereports/{0}";
        }

        internal static class Issues
        {
            internal const string IsConnected = "/api/v1/issues/{0}/checkconnection";
            internal const string GetIssueUrl = "/api/v1/issues/{0}/{1}/{2}";
            internal const string GetIssueField = "/api/v1/issues/{0}/{1}/{2}/{3}";
            internal const string SetIssueField = "/api/v1/issues/{0}/{1}/{2}/{3}";
        }

        internal static class Notify
        {
            internal const string NotifyMessage = "/api/v1/notify/{0}";
        }

        internal class CI
        {
            internal const string LaunchPlan = "/api/v1/ci/{0}/{1}";
            internal const string GetPlanStatus = "/api/v1/ci/{0}/{1}/{2}";
        }
    }
}
