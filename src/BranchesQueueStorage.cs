using System.Collections.Generic;

using Codice.LogWrapper;

namespace TrunkBot
{
    internal class BranchesQueueStorage
    {
        internal void AddQueuedBranches(List<Branch> branches)
        {
            lock(mLock)
                foreach (Branch branch in branches)
                    mBranches.AddFirst(branch);
        }

        internal void EnqueueBranchIfNotAlreadyAdded(Branch branch)
        {
            lock (mLock)
            {
                if (BranchFinder.Find(mBranches, branch.Repository, branch.Id) != null)
                    return;
                mBranches.AddFirst(branch);
            }
        }

        internal void EnqueueBranchOnTop(Branch branch)
        {
            lock (mLock)
                mBranches.AddLast(branch);
        }

        internal Branch DequeueBranch()
        {
            lock (mLock)
            {
                if (mBranches.Count == 0)
                    return null;

                Branch result = mBranches.Last.Value;
                mBranches.RemoveLast();
                return result;
            }
        }

        internal Branch PeekBranch()
        {
            lock (mLock)
            {
                if (mBranches.Count == 0)
                    return null;

                return mBranches.Last.Value;
            }
        }

        internal void RemoveBranch(string repository, int branchId)
        {
            lock (mLock)
            {
                Branch branch = BranchFinder.Find(mBranches, repository, branchId);
                if (branch == null)
                    return;
                mBranches.Remove(branch);
            }
        }

        static class BranchFinder
        {
            internal static Branch Find(
                LinkedList<Branch> branches, string repository, int branchId)
            {
                foreach(Branch branch in branches)
                {
                    if (branch.Id != branchId)
                        continue;

                    if (!RepositoryNameComparer.IsSameName(branch.Repository, repository))
                        continue;

                    return branch;
                }

                return null;
            }
        }

        LinkedList<Branch> mBranches = new LinkedList<Branch>();
        object mLock = new object();

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
