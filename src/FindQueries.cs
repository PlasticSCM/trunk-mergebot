﻿using System;
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
                "branch where ( name like '{0}%' or name like '{1}%' ) " +
                "and date > '{2}' and attribute='{3}' and ( attrvalue='{4}' or attrvalue='{5}')",
                prefix.ToLowerInvariant(),
                prefix.ToUpperInvariant(),
                DateTime.Now.AddYears(-1).ToString(DATE_FORMAT),
                statusAttributeName,
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

        const string DATE_FORMAT = "yyyy-MM-dd HH:mm:ss";
    }
}
