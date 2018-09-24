using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;

using log4net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;

namespace TrunkBot.Api
{
    internal class RestApi
    {
        internal UsersApi Users { get; private set; }
        internal MergeReportsApi MergeReports { get; private set; }
        internal IssuesApi Issues { get; private set; }
        internal NotifyApi Notify { get; private set; }
        internal CIApi CI { get; private set; }

        internal RestApi(string serverUrl, string apiKey)
        {
            mBaseUri = new Uri(serverUrl);
            mApiKey = apiKey;

            Users = new UsersApi(mBaseUri, apiKey);
            MergeReports = new MergeReportsApi(mBaseUri, apiKey);
            Issues = new IssuesApi(mBaseUri, apiKey);
            Notify = new NotifyApi(mBaseUri, apiKey);
            CI = new CIApi(mBaseUri, apiKey);
        }

        internal BranchModel GetBranch(
            string repoName, string branchName)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetBranch,
                repoName, FormatTargetName(branchName));

            return Internal.MakeApiRequest<BranchModel>(
                endpoint, HttpMethod.Get, mApiKey);
        }

        internal ChangesetModel GetChangeset(string repoName, int changesetId)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetChangeset, repoName, changesetId.ToString());

            return Internal.MakeApiRequest<ChangesetModel>(
                endpoint, HttpMethod.Get, mApiKey);
        }

        internal SingleResponse GetAttribute(
            string repoName, string attributeName, AttributeTargetType targetType, string targetName)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.GetAttribute,
                repoName, attributeName, targetType.ToString(),
                FormatTargetName(targetName));

            return Internal.MakeApiRequest<SingleResponse>(
                endpoint, HttpMethod.Get, mApiKey);
        }

        internal void ChangeAttribute(
            string repoName, string attributeName, ChangeAttributeRequest request)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.ChangeAttribute,
                repoName, attributeName);

            Internal.MakeApiRequest<ChangeAttributeRequest>(
                endpoint, HttpMethod.Put, request, mApiKey);
        }

        internal MergeToResponse MergeTo(
            string repoName, MergeToRequest request)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.MergeTo, repoName);

            return Internal.MakeApiRequest<MergeToRequest, MergeToResponse>(
                endpoint, HttpMethod.Post, request, mApiKey);
        }

        internal void DeleteShelve(string repoName, int shelveId)
        {
            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.DeleteShelve,
                repoName, shelveId.ToString());

            Internal.MakeApiRequest(
                endpoint, HttpMethod.Delete, mApiKey);
        }

        internal JArray Find(
            string repoName, string query, string queryDateFormat, string[] fields)
        {
            string fieldsQuery = string.Empty;
            if (fields != null && fields.Length > 0)
                fieldsQuery = string.Join(",", fields);

            Uri endpoint = ApiUris.GetFullUri(
                mBaseUri, ApiEndpoints.Find, repoName, query, queryDateFormat, fieldsQuery);

            return Internal.MakeApiRequest<JArray>(
                endpoint, HttpMethod.Get, mApiKey);
        }

        static string FormatTargetName(string targetName)
        {
            if (targetName.StartsWith("/"))
                return targetName.Substring(1);

            return targetName;
        }

        static string FormatDate(DateTime timestamp)
        {
            return timestamp.ToString("yyyy-MM-dd hh:mm");
        }

        internal class UsersApi
        {
            internal UsersApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            internal JObject GetUserProfile(string name)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Users.GetUserProfile, name);

                return Internal.MakeApiRequest<JObject>(
                    endpoint, HttpMethod.Get, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class MergeReportsApi
        {
            internal MergeReportsApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            internal void ReportMerge(
                string mergebotName, MergeReport mergeReport)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.MergeReports.ReportMerge,
                    mergebotName);

                Internal.MakeApiRequest<MergeReport>(
                    endpoint, HttpMethod.Put, mergeReport, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class IssuesApi
        {
            internal IssuesApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            internal SingleResponse IsConnected(
                string issueTrackerName)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.IsConnected,
                    issueTrackerName);

                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, mApiKey);
            }

            internal SingleResponse GetIssueUrl(
                string issueTrackerName, string projectKey, string taskNumber)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.GetIssueUrl,
                    issueTrackerName, projectKey, taskNumber);

                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, mApiKey);
            }

            internal SingleResponse GetIssueField(
                string issueTrackerName, string projectKey, string taskNumber, string fieldName)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.GetIssueField,
                    issueTrackerName, projectKey, taskNumber, fieldName);

                return Internal.MakeApiRequest<SingleResponse>(
                    endpoint, HttpMethod.Get, mApiKey);
            }

            internal SingleResponse SetIssueField(
                string issueTrackerName, string projectKey, string taskNumber, string fieldName,
                SetIssueFieldRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Issues.SetIssueField,
                    issueTrackerName, projectKey, taskNumber, fieldName);

                return Internal.MakeApiRequest<SetIssueFieldRequest, SingleResponse>(
                    endpoint, HttpMethod.Put, request, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class NotifyApi
        {
            internal NotifyApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            internal void NotifyMessage(
                string notifierName, NotifyMessageRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.Notify.NotifyMessage,
                    notifierName);

                Internal.MakeApiRequest<NotifyMessageRequest>(
                    endpoint, HttpMethod.Post, request, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        internal class CIApi
        {
            internal CIApi(Uri baseUri, string apiKey)
            {
                mBaseUri = baseUri;
                mApiKey = apiKey;
            }

            internal SingleResponse LaunchPlan(
                string ciName, string planName, LaunchPlanRequest request)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.CI.LaunchPlan,
                    ciName, planName);

                return Internal.MakeApiRequest<LaunchPlanRequest, SingleResponse>(
                    endpoint, HttpMethod.Post, request, mApiKey);
            }

            internal GetPlanStatusResponse GetPlanStatus(
                string ciName, string planName, string buildId)
            {
                Uri endpoint = ApiUris.GetFullUri(
                    mBaseUri, ApiEndpoints.CI.GetPlanStatus,
                    ciName, planName, buildId);

                return Internal.MakeApiRequest<GetPlanStatusResponse>(
                    endpoint, HttpMethod.Get, mApiKey);
            }

            readonly Uri mBaseUri;
            readonly string mApiKey;
        }

        static class Internal
        {
            internal static void MakeApiRequest(
                Uri endpoint, HttpMethod httpMethod, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest(
                        endpoint, httpMethod, apiKey);

                    ConsumeResponse(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(endpoint, ex);
                }
                catch (Exception ex)
                {
                    LogException(endpoint, ex.Message, ex.StackTrace);
                    throw;
                }
            }

            internal static void MakeApiRequest<TReq>(
                Uri endpoint, HttpMethod httpMethod, TReq body, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest<TReq>(
                        endpoint, httpMethod, body, apiKey);

                    ConsumeResponse(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(endpoint, ex);
                }
                catch (Exception ex)
                {
                    LogException(endpoint, ex.Message, ex.StackTrace);
                    throw;
                }
            }

            internal static TRes MakeApiRequest<TRes>(
                Uri endpoint, HttpMethod httpMethod, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest(
                        endpoint, httpMethod, apiKey);

                    return GetResponse<TRes>(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(endpoint, ex);
                }
                catch (Exception ex)
                {
                    LogException(endpoint, ex.Message, ex.StackTrace);
                    throw;
                }
            }

            internal static TRes MakeApiRequest<TReq, TRes>(
                Uri endpoint, HttpMethod httpMethod, TReq body, string apiKey)
            {
                try
                {
                    HttpWebRequest request = CreateWebRequest<TReq>(
                        endpoint, httpMethod, body, apiKey);

                    return GetResponse<TRes>(request);
                }
                catch (WebException ex)
                {
                    throw WebServiceException.AdaptException(endpoint, ex);
                }
                catch (Exception ex)
                {
                    LogException(endpoint, ex.Message, ex.StackTrace);
                    throw;
                }
            }

            static HttpWebRequest CreateWebRequest(
                Uri endpoint, HttpMethod httpMethod, string apiKey)
            {
                HttpWebRequest request = WebRequest.CreateHttp(endpoint);
                request.Method = httpMethod.Method;
                SetApiKeyAuth(request, apiKey);

                request.ContentLength = 0;

                return request;
            }

            static HttpWebRequest CreateWebRequest<TReq>(
                Uri endpoint, HttpMethod httpMethod, TReq body, string apiKey)
            {
                HttpWebRequest request = WebRequest.CreateHttp(endpoint);
                request.Method = httpMethod.Method;
                request.ContentType = "application/json";
                SetApiKeyAuth(request, apiKey);

                WriteBody(request, body);

                return request;
            }

            static void SetApiKeyAuth(HttpWebRequest request, string apiKey)
            {
                request.Headers["Authorization"] = "ApiKey " + apiKey;
            }

            static void WriteBody(WebRequest request, object body)
            {
                using (Stream st = request.GetRequestStream())
                using (StreamWriter writer = new StreamWriter(st))
                {
                    writer.Write(JsonConvert.SerializeObject(body));
                }
            }

            static TRes GetResponse<TRes>(WebRequest request)
            {
                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<TRes>(reader.ReadToEnd());
                }
            }

            static void ConsumeResponse(WebRequest request)
            {
                using (WebResponse response = request.GetResponse())
                {
                    // Discard the response body
                }
            }
        }

        static void LogException(Uri endpoint, string message, string stackTrace)
        {
            mLog.ErrorFormat("There was an error while calling '{0}': {1}", endpoint, message);
            mLog.DebugFormat("StackTrace:{0}{1}", Environment.NewLine, stackTrace);
        }

        static class WebServiceException
        {
            internal static Exception AdaptException(Uri endpoint, WebException ex)
            {
                string message = GetExceptionMessage(endpoint, ex);
                LogException(endpoint, message, ex.StackTrace);

                return new Exception(message);
            }

            static string GetExceptionMessage(Uri endpoint, WebException ex)
            {
                HttpWebResponse response = ex.Response as HttpWebResponse;
                if (response == null)
                    return ex.Message;

                try
                {
                    return ReadErrorMessageFromRespose(response);
                }
                catch (Exception e)
                {
                    mLog.ErrorFormat(
                        "There was an error reading the exception error " +
                        "message for request '{0}'. Error: {1} ", endpoint, e.Message);
                    return ex.Message;
                }
            }

            static string ReadErrorMessageFromRespose(HttpWebResponse response)
            {
                using (StreamReader resultStream =
                    new StreamReader(response.GetResponseStream()))
                {
                    JObject jObj = JsonConvert.DeserializeObject<JObject>(
                        resultStream.ReadToEnd());

                    return jObj.Value<JObject>("error").Value<string>("message");
                }
            }
        }

        readonly Uri mBaseUri;
        readonly string mApiKey;

        static readonly ILog mLog = LogManager.GetLogger("RestApi");
    }
}
