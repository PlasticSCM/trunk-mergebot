using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TrunkBot;

namespace Codice.CM.Server.Devops
{
    public class Label
    {
        internal readonly string Name;
        internal readonly DateTime Date;
        internal readonly int Changeset;

        public Label(string name, DateTime date, int changeset)
        {
            Name = name;
            Date = date;
            Changeset = changeset;
        }
    }
    
    class ChangesetModel
    {
        public int Id { get; set; }
        public int ChangesetId { get; set; }
        public string RepositoryId { get; set; }
        public int ParentChangesetId { get; set; }
        public string Branch { get; set; }
        public DateTime Date { get; set; }
        public Guid Guid { get; set; }
        public string Owner { get; set; }
        public string Comment { get; set; }
        public string Type { get; set; }
    }    
    
    [Flags]
    internal enum MergeToOptions : byte
    {
        None = 0,
        CreateShelve = 1 << 0,
        EnsureNoDstChanges = 1 << 1,
        // TODO: handle this flag in the IRepositoryOperationsForMergebot implementation (server side)
        SkipMergeRulesCheckCreatingShelve = 1 << 2
    }

    internal interface IRepositoryOperationsForMergebot
    {
        Task<string> GetBranchAttributeValue(string repoName, string branchName, string attributeName);
        
        Task ChangeBranchAttributeValue(
            string repoName, string branchName, string attributeName, string attributeValue);

        Task<string> GetChangesetAttributeValue(string repoName, int csetId, string attributeName);
        
        Task ChangeChangesetAttributeValue(
            string repoName, int csetId, string attributeName, string attributeValue);        
        
        Task CreateLabel(string repoName, string labelName, int csetId, string comment);

        Task<bool> TryCreateAttribute(
            string repoName, string attributeName, string attributeComment);

        Task<Branch> GetBranch(string repoName, string branchName, CancellationToken ct);
        
        Task<string> GetBranchName(string repoName, int branchId, CancellationToken ct);

        Task<string> GetBranchRepId(
            string repoName, string branchName, CancellationToken ct);
        
        Task<int> GetBranchId(string repoName, string branchName);

        Task<int> GetBranchHead(string repoName, string branchName, CancellationToken ct);
        
        Task<int> GetParentChangesetId(string repoName, int csetToQuery);

        Task<ChangesetModel> GetChangeset(
            string repoName, int changesetId, CancellationToken ct);

        Task<List<Branch>> FindResolvedBranches(
            string repoName,
            string prefix,
            string statusAttributeName,
            string resolvedStatusAttributeValue,
            CancellationToken ct);

        Task<List<BranchWithReview>> FindPendingBranchesWithReviews(
            string repoName,
            string prefix,
            string statusAttributeName,
            string mergedStatusAttributeValue,
            CancellationToken ct);
        
        Task<List<Label>> FindLabelsBetweenDate(string repoName, DateTime start, DateTime end);

        Task<Label> FindLastPublishedLabel(
            string repoName,
            string prefix,
            string plasticPublishedAttrName,
            string publishedStatusAttrValue);        

        Task<bool> ExistsAttributeName(string repoName, string attributeName);

        Task<Label> FindMostRecentLabel(
            string repoName, DateTime limitQuerySince, string pattern);

        Task<Label> FindLastLabelInBranch(
            string repoName, string branchName, string prefix);

        Task<List<string>> FindIntegratedBranchesFromDate(
            string repoName, string branchName, DateTime date);

        Task<MergeToResponse> MergeBranchTo(
            string repoName,
            string sourceBranch,
            string destinationBranch,
            string comment,
            MergeToOptions options);

        Task<MergeToResponse> MergeShelveTo(
            string repoName,
            int shelveId,
            string destinationBranch,
            string comment,
            MergeToOptions options);

        Task DeleteShelve(string repoName, int shelveId);

        Task<bool> IsMergeAllowed(
            string repoName,
            string sourceBranchName,
            string destinationBranchName,
            CancellationToken ct);

        Task UpdateCodeReview(string repoName, int reviewId, int newStatus, string newTitle);

        Task<List<string>> DiffShelve(string repoName, int shelveId);

        Task<List<string>> DiffChangesets(string repoName, int srcCsetId, int dstCsetId);
    }
}