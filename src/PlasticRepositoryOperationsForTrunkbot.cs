using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

using Codice.CM.Server.Devops;
using TrunkBot.Api;
using TrunkBot.Api.Requests;
using TrunkBot.Api.Responses;

namespace TrunkBot
{
    public class MergeToAllowedResponse
    {
        public string Result { get; set; }
    }
    
    public class RepositoryOperationsForTrunkbot : IRepositoryOperationsForMergebot
    {
        internal RepositoryOperationsForTrunkbot(RestApi restApi)
        {
            mRestApi = restApi;
        }

        async Task<string> IRepositoryOperationsForMergebot.GetBranchAttributeValue(
            string repoName, string branchName, string attributeName)
        {
            return (await mRestApi.GetAttribute(repoName, attributeName,
                AttributeTargetType.Branch, branchName)).Value;
        }

        async Task IRepositoryOperationsForMergebot.ChangeBranchAttributeValue(
            string repoName, string branchName, string attributeName, string attributeValue)
        {
            ChangeAttributeRequest request = new ChangeAttributeRequest()
            {
                TargetType = AttributeTargetType.Branch,
                TargetName = branchName,
                Value = attributeValue
            };

            await mRestApi.ChangeAttribute(repoName, attributeName, request);
        }

        Task<string> IRepositoryOperationsForMergebot.GetChangesetAttributeValue(
            string repoName, int csetId, string attributeName)
        {
            throw new NotImplementedException();
        }

        Task IRepositoryOperationsForMergebot.ChangeChangesetAttributeValue(
            string repoName, int csetId, string attributeName,
            string attributeValue)
        {
            throw new NotImplementedException();
        }

        async Task IRepositoryOperationsForMergebot.CreateLabel(
            string repoName, string labelName, int csetId, string comment)
        {
            CreateLabelRequest request = new CreateLabelRequest()
            {
                Name = labelName,
                Changeset = csetId,
                Comment = comment
            };

            await mRestApi.Labels.Create(repoName, request);
        }

        async Task<bool> IRepositoryOperationsForMergebot.TryCreateAttribute(
            string repoName, 
            string attributeName,
            string attributeComment)
        {
            CreateAttributeRequest request = new CreateAttributeRequest()
            {
                Name = attributeName,
                Comment = attributeComment
            };

            SingleResponse response = await mRestApi.Attributes.Create(repoName, request);

            return GetBoolValue(response.Value, false);
        }
        
        async Task<Branch> IRepositoryOperationsForMergebot.GetBranch(
            string repoName, string branchName, CancellationToken ct)
        {
            BranchModel model = await mRestApi.GetBranch(repoName, branchName);
            return new Branch(repoName, model.Id, branchName, model.Owner, model.Comment);
        }

        async Task<string> IRepositoryOperationsForMergebot.GetBranchRepId(
            string repoName, string branchName, CancellationToken ct)
        {
            return (await mRestApi.GetBranch(repoName, branchName)).RepositoryId;
        }

        Task<int> IRepositoryOperationsForMergebot.GetBranchId(string repoName, string branchName)
        {
            throw new NotImplementedException();
        }

        async Task<int> IRepositoryOperationsForMergebot.GetBranchHead(
            string repoName, string branchName, CancellationToken ct)
        {
            return (await mRestApi.GetBranch(repoName, branchName)).HeadChangeset;
        }

        Task<int> IRepositoryOperationsForMergebot.GetParentChangesetId(string repoName, int csetToQuery)
        {
            throw new NotImplementedException();
        }

        async Task<ChangesetModel> IRepositoryOperationsForMergebot.GetChangeset(
            string repoName, int changesetId, CancellationToken ct)
        {
            return await mRestApi.GetChangeset(repoName, changesetId);
        }        
        
        async Task<string> IRepositoryOperationsForMergebot.GetBranchName(
            string repository, int branchId, CancellationToken ct)
        {
            return await FindQueries.GetBranchName(mRestApi, repository, branchId);
        }

        async Task<List<Branch>> IRepositoryOperationsForMergebot.FindResolvedBranches(
            string repository,
            string prefix,
            string statusAttributeName,
            string resolvedStatusAttributeValue,
            CancellationToken ct)
        {
            return await FindQueries.FindResolvedBranches(
                mRestApi, repository, prefix, statusAttributeName, resolvedStatusAttributeValue);
        }

        async Task<List<BranchWithReview>> IRepositoryOperationsForMergebot.FindPendingBranchesWithReviews(
            string repository,
            string prefix,
            string statusAttributeName,
            string mergedStatusAttributeValue,
            CancellationToken ct)
        {
            return await FindQueries.FindPendingBranchesWithReviews(
                mRestApi, repository, prefix, statusAttributeName, mergedStatusAttributeValue);
        }

        Task<List<Label>> IRepositoryOperationsForMergebot.FindLabelsBetweenDate(
            string repoName, DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        Task<Label> IRepositoryOperationsForMergebot.FindLastPublishedLabel(
            string repoName, string prefix, string plasticPublishedAttrName,
            string publishedStatusAttrValue)
        {
            throw new NotImplementedException();
        }

        async Task<bool> IRepositoryOperationsForMergebot.ExistsAttributeName(
            string repository, string attributeName)
        {
            return await FindQueries.ExistsAttributeName(mRestApi, repository, attributeName);
        }

        async Task<Label> IRepositoryOperationsForMergebot.FindMostRecentLabel(
            string repository, DateTime limitQuerySince, string pattern)
        {
            return await FindQueries.FindMostRecentLabel(mRestApi, repository, limitQuerySince, pattern);
        }

        Task<Label> IRepositoryOperationsForMergebot.FindLastLabelInBranch(
            string repoName, string branchName, string prefix)
        {
            throw new NotImplementedException();
        }

        Task<List<string>> IRepositoryOperationsForMergebot.FindIntegratedBranchesFromDate(
            string repoName, string branchName, DateTime date)
        {
            throw new NotImplementedException();
        }

        async Task<MergeToResponse> IRepositoryOperationsForMergebot.MergeBranchTo(
            string repoName,
            string sourceBranch,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return await Merge.MergeBranchTo(
                mRestApi, repoName, sourceBranch, destinationBranch, comment, options);
        }

        async Task<MergeToResponse> IRepositoryOperationsForMergebot.MergeShelveTo(
            string repoName,
            int shelveId,
            string destinationBranch,
            string comment,
            MergeToOptions options)
        {
            return await Merge.MergeShelveTo(
                mRestApi, repoName, shelveId, destinationBranch, comment, options);
        }

        async Task IRepositoryOperationsForMergebot.DeleteShelve(string repoName, int shelveId)
        {
            await Merge.DeleteShelve(mRestApi, repoName, shelveId);
        }

        async Task<bool> IRepositoryOperationsForMergebot.IsMergeAllowed(
            string repoName,
            string sourceBranchName,
            string destinationBranchName,
            CancellationToken ct)
        {
            return await Merge.IsMergeAllowed(
                mRestApi, repoName, sourceBranchName, destinationBranchName);
        }

        async Task IRepositoryOperationsForMergebot.UpdateCodeReview(
            string repoName, int reviewId, int newStatus, string newTitle)
        {
            UpdateReviewRequest request = new UpdateReviewRequest()
            {
                Status = newStatus,
                Title = newTitle
            };

            await mRestApi.CodeReviews.UpdateReview(repoName, reviewId, request);
        }

        Task<List<string>> IRepositoryOperationsForMergebot.DiffShelve(string repoName, int shelveId)
        {
            throw new NotImplementedException();
        }

        Task<List<string>> IRepositoryOperationsForMergebot.DiffChangesets(
            string repoName, int srcCsetId, int dstCsetId)
        {
            throw new NotImplementedException();
        }

        static bool GetBoolValue(string value, bool defaultValue)
        {
            bool flag;
            return Boolean.TryParse(value, out flag) ? flag : defaultValue;
        }

        static class Merge
        {
            internal static async Task<MergeToResponse> MergeBranchTo(
                RestApi restApi,
                string repoName,
                string sourceBranch,
                string destinationBranch,
                string comment,
                MergeToOptions options)
            {
                return await MergeTo(
                    restApi, repoName, sourceBranch, MergeToSourceType.Branch,
                    destinationBranch, comment, options);
            }

            internal static async Task<MergeToResponse> MergeShelveTo(
                RestApi restApi,
                string repoName,
                int shelveId,
                string destinationBranch,
                string comment,
                MergeToOptions options)
            {
                return await MergeTo(
                    restApi, repoName, shelveId.ToString(), MergeToSourceType.Shelve,
                    destinationBranch, comment, options);
            }

            internal static async Task DeleteShelve(
                RestApi restApi,
                string repoName, int shelveId)
            {
                await restApi.DeleteShelve(repoName, shelveId);
            }

            internal static async Task<bool> IsMergeAllowed(
                RestApi restApi,
                string repoName,
                string sourceBranchName,
                string destinationBranchName)
            {
                MergeToAllowedResponse response = await restApi.IsMergeAllowed(
                    repoName, sourceBranchName, destinationBranchName);

                return
                    response.Result.Trim()
                        .Equals("ok", StringComparison.InvariantCultureIgnoreCase);
            }

            static async Task<MergeToResponse> MergeTo(
                RestApi restApi,
                string repoName,
                string source,
                MergeToSourceType sourceType,
                string destinationBranch,
                string comment,
                MergeToOptions options)
            {
                MergeToRequest request = new MergeToRequest()
                {
                    SourceType = sourceType,
                    Source = source,
                    Destination = destinationBranch,
                    Comment = comment,
                    CreateShelve = options.HasFlag(MergeToOptions.CreateShelve),
                    EnsureNoDstChanges = options.HasFlag(MergeToOptions.EnsureNoDstChanges)
                };

                return await restApi.MergeTo(repoName, request);
            }            
        }
        
        static class FindQueries
        {
            internal static async Task<string> GetBranchName(RestApi restApi, string repository, int branchId)
            {
                string query = string.Format("branch where id={0}", branchId);

                JArray findResult = await Find(
                    restApi,
                    repository,
                    query,
                    DATE_FORMAT,
                    "retrieve a single branch by ID",
                    new string[] {"name"});

                if (findResult.Count == 0)
                    return string.Empty;

                return GetStringValue((JObject) findResult[0], "name");
            }

            internal static async Task<List<Branch>> FindResolvedBranches(
                RestApi restApi,
                string repository,
                string prefix,
                string statusAttributeName,
                string resolvedStatusAttributeValue)
            {
                string query = string.Format(
                    "branch where ( name like '{0}%' or name like '{1}%' or name like '{2}%' ) " +
                    "and date > '{3}' " +
                    "and attribute='{4}' and ( attrvalue='{5}' or attrvalue='{6}' or attrvalue='{7}') ",
                    prefix,
                    prefix.ToLowerInvariant(),
                    prefix.ToUpperInvariant(),
                    DateTime.Now.AddYears(-1).ToString(DATE_FORMAT),
                    statusAttributeName,
                    resolvedStatusAttributeValue,
                    resolvedStatusAttributeValue.ToLowerInvariant(),
                    resolvedStatusAttributeValue.ToUpperInvariant());

                JArray findResult = await Find(
                    restApi,
                    repository,
                    query,
                    DATE_FORMAT,
                    "retrieve the list of branches to process",
                    new string[] {"id", "name", "owner", "comment"});

                List<Branch> result = new List<Branch>();
                foreach (JObject obj in findResult)
                {
                    result.Add(new Branch(
                        repository,
                        GetIntValue(obj, "id"),
                        GetStringValue(obj, "name"),
                        GetStringValue(obj, "owner"),
                        GetStringValue(obj, "comment")));
                }

                return result;
            }

            internal static async Task<List<BranchWithReview>> FindPendingBranchesWithReviews(
                RestApi restApi,
                string repository,
                string prefix,
                string statusAttributeName,
                string mergedStatusAttributeValue)
            {
                string reviewTypeConditionClause = string.Empty;

                //branches from a year ago matching with prefix with status!=merged (even those without any status set)
                string branchTypeConditionClause = string.Format(
                    "( " +
                    "    name like '{0}%' or name like '{1}%' or name like '{2}%' " +
                    ") " +
                    "and " +
                    "( " +
                    "    date > '{3}' " +
                    ") " +
                    "and " +
                    "( " +
                    "    (not attribute='{4}') or " +
                    "    (attribute='{4}' and not ( attrvalue='{5}' or attrvalue='{6}' or attrvalue='{7}' )) " +
                    ") ",
                    prefix,
                    prefix.ToUpperInvariant(),
                    prefix.ToLowerInvariant(),
                    DateTime.Now.AddYears(-1).ToString(DATE_FORMAT),
                    statusAttributeName,
                    mergedStatusAttributeValue,
                    mergedStatusAttributeValue.ToUpperInvariant(),
                    mergedStatusAttributeValue.ToLowerInvariant());

                string[] outputFields = new string[]
                {
                    "branchid", "branchname", "branchowner", "branchcomment",
                    "reviewid", "reviewtargetid", "reviewstatus", "reviewtitle"
                };

                JArray findResult = await FindBranchesWithReviews(
                    restApi,
                    repository,
                    reviewTypeConditionClause,
                    branchTypeConditionClause,
                    DATE_FORMAT,
                    "retrieve the list of branches with reviews to process",
                    outputFields);

                List<BranchWithReview> result = new List<BranchWithReview>();
                Branch branch = null;
                Review review = null;

                foreach (JObject obj in findResult)
                {
                    branch = new Branch(
                        repository,
                        GetIntValue(obj, "branchid"),
                        GetStringValue(obj, "branchname"),
                        GetStringValue(obj, "branchowner"),
                        GetStringValue(obj, "branchcomment"));

                    review = new Review(
                        repository,
                        GetIntValue(obj, "reviewid"),
                        GetIntValue(obj, "reviewtargetid"),
                        TranslateCodeReviewStatus(GetStringValue(obj, "reviewstatus")),
                        GetStringValue(obj, "reviewtitle"));

                    result.Add(new BranchWithReview()
                    {
                        Branch = branch,
                        Review = review
                    });
                }

                return result;
            }

            internal static async Task<bool> ExistsAttributeName(
                RestApi restApi,
                string repository,
                string attributeName)
            {
                string query = string.Format("attributetype where name='{0}' ", attributeName);

                JArray findResult = await Find(
                    restApi,
                    repository,
                    query,
                    DATE_FORMAT,
                    "retrieve the list of attributes named " + attributeName,
                    new string[] {"name"});

                return findResult != null && findResult.Count > 0;
            }

            internal static async Task<Label> FindMostRecentLabel(
                RestApi restApi,
                string repository,
                DateTime limitQuerySince,
                string pattern)
            {
                string query = string.Format(
                    "marker where name like '{0}' {1}",
                    pattern,
                    limitQuerySince == DateTime.MinValue
                        ? string.Empty
                        : "and date > '" + limitQuerySince.ToString(DATE_FORMAT) + "'");

                return await GetMostRecentLabelFromQuery(
                    restApi, repository, query, "find last label matching a pattern");
            }

            static async Task<Label> GetMostRecentLabelFromQuery(RestApi restApi, string repository,
                string query, string actionDescription)
            {
                JArray findResult = await restApi.Find(
                    repository,
                    query,
                    DATE_FORMAT,
                    actionDescription,
                    new string[] {"name", "date", "changeset"});

                Label result = null;

                foreach (JObject obj in findResult)
                {
                    if (obj["date"] == null)
                        continue;

                    DateTime timestamp = obj["date"].Value<DateTime>();
                    string name = GetStringValue(obj, "name");
                    int changeset = GetIntValue(obj, "changeset");

                    if (result == null)
                    {
                        result = new Label(name, timestamp, changeset);
                        continue;
                    }

                    if (timestamp < result.Date)
                        continue;

                    result = new Label(name, timestamp, changeset);
                }

                return result;
            }

            static async Task<JArray> Find(
                RestApi restApi,
                string repName,
                string query,
                string queryDateFormat,
                string actionDescription,
                string[] fields)
            {
                return await restApi.Find(repName, query, queryDateFormat, actionDescription, fields);
            }

            static async Task<JArray> FindBranchesWithReviews(
                RestApi restApi,
                string repName,
                string reviewConditions,
                string branchConditions,
                string queryDateFormat,
                string actionDescription,
                string[] fields)
            {
                return await restApi.FindBranchesWithReviews(
                    repName, 
                    reviewConditions, 
                    branchConditions, 
                    queryDateFormat, 
                    actionDescription, 
                    fields);
            }            
            
            static string GetStringValue(JObject obj, string fieldName)
            {
                object value = obj[fieldName];
                return value == null ? string.Empty : value.ToString();
            }
            
            static int GetIntValue(JObject obj, string fieldName)
            {
                object value = obj[fieldName];
                return value == null ? 0 : Convert.ToInt32(value);
            }            

            static string TranslateCodeReviewStatus(string reviewStatusId)
            {
                if (string.IsNullOrEmpty(reviewStatusId))
                    return string.Empty;

                int parsedInt = -1;
                if (!int.TryParse(reviewStatusId, out parsedInt))
                    return reviewStatusId;

                return Review.ParseStatusId(parsedInt);
            }

            const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
        }

        readonly RestApi mRestApi;
    }
}