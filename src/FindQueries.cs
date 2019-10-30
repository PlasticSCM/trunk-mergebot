using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using TrunkBot.Api;
using TrunkBot.Labeling;

namespace TrunkBot
{
    internal static class FindQueries
    {
        internal static string GetBranchName(
            RestApi restApi, string repository, string branchId)
        {
            string query = string.Format("branch where id={0}", branchId);

            JArray findResult = TrunkMergebotApi.Find(
                restApi,
                repository,
                query,
                DATE_FORMAT,
                "retrieve a single branch by ID",
                new string[] { "name" });

            if (findResult.Count == 0)
                return string.Empty;

            return GetStringValue((JObject)findResult[0], "name");
        }

        internal static List<Branch> FindResolvedBranches(
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

            JArray findResult = TrunkMergebotApi.Find(
                restApi,
                repository,
                query,
                DATE_FORMAT,
                "retrieve the list of branches to process",
                new string[] { "id", "name", "owner", "comment" });

            List<Branch> result = new List<Branch>();
            foreach(JObject obj in findResult)
            {
                result.Add(new Branch(
                    repository,
                    GetStringValue(obj, "id"),
                    GetStringValue(obj, "name"),
                    GetStringValue(obj, "owner"),
                    GetStringValue(obj, "comment")));
            }
            return result;
        }

        internal static List<BranchWithReview> FindPendingBranchesWithReviews(
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
                {"branchid", "branchname","branchowner","branchcomment",
                "reviewid","reviewtargetid","reviewstatus","reviewtitle"};

            JArray findResult = TrunkMergebotApi.FindBranchesWithReviews(
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
                    GetStringValue(obj, "branchid"),
                    GetStringValue(obj, "branchname"),
                    GetStringValue(obj, "branchowner"),
                    GetStringValue(obj, "branchcomment"));

                review = new Review(
                    repository,
                    GetStringValue(obj, "reviewid"),
                    GetStringValue(obj, "reviewtargetid"),
                    TranslateCodeReviewStatus(GetStringValue(obj, "reviewstatus")),
                    GetStringValue(obj, "reviewtitle"));

                result.Add(new BranchWithReview()
                { Branch = branch,
                  Review = review
                });
            }

            return result;
        }

        internal static bool ExistsAttributeName(
            RestApi restApi,
            string repository,
            string attributeName)
        {
            string query = string.Format("attributetype where name='{0}' ", attributeName);

            JArray findResult = TrunkMergebotApi.Find(
                restApi,
                repository,
                query,
                DATE_FORMAT,
                "retrieve the list of attributes named " + attributeName,
                new string[] { "name" });

            return findResult != null && findResult.Count > 0;
        }

        internal static Label FindMostRecentLabel(
            RestApi restApi,
            string repository,
            DateTime limitQuerySince,
            string pattern)
        {
            string query = string.Format(
                "marker where name like '{0}' {1}",
                pattern,
                limitQuerySince == DateTime.MinValue ?
                    string.Empty :
                    "and date > '" + limitQuerySince.ToString(DATE_FORMAT) + "'");

            return GetMostRecentLabelFromQuery(
                restApi, repository, query, "find last label matching a pattern");
        }

        static Label GetMostRecentLabelFromQuery(RestApi restApi, string repository, string query, string actionDescription)
        {
            JArray findResult = restApi.Find(
                repository,
                query,
                DATE_FORMAT,
                actionDescription,
                new string[] { "name", "date" });

            Label result = null;

            foreach (JObject obj in findResult)
            {
                if (obj["date"] == null)
                    continue;

                DateTime timestamp = obj["date"].Value<DateTime>();
                string name = GetStringValue(obj, "name");

                if (result == null)
                {
                    result = new Label(name, timestamp);
                    continue;
                }

                if (timestamp < result.Date)
                    continue;

                result.Name = name;
                result.Date = timestamp;
            }

            return result;
        }

        static string GetStringValue(JObject obj, string fieldName)
        {
            object value = obj[fieldName];
            return value == null ? string.Empty : value.ToString();
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
}
