using System;
using System.Collections.Generic;
using System.IO;

using log4net;

namespace TrunkBot
{
    internal static class BranchesQueueStorage
    {
        internal static bool HasQueuedBranches(string filePath)
        {
            return GetQueuedBranches(filePath).Count > 0;
        }

        internal static bool Contains(string repository, string branchId, string filePath)
        {
            List<Branch> branches = GetQueuedBranches(filePath);

            return BranchFinder.IndexOf(branches, repository, branchId) > -1;
        }

        internal static void EnqueueBranch(Branch branch, string filePath)
        {
            string line = string.Empty;
            try
            {
                line = BranchParser.ToString(branch);

                if (Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                using (StreamWriter file = new StreamWriter(filePath, true))
                {
                    file.WriteLine(line);
                }
            }
            catch (Exception ex)
            {
                LogException("Error saving branch '{0}' to '{1}': {2}", ex, branch.FullName, filePath);
            }
        }

        internal static Branch DequeueBranch(string filePath)
        {
            List<Branch> queuedBranches = GetQueuedBranches(filePath);

            if (queuedBranches.Count == 0)
                return null;

            Branch dequeueBranch = queuedBranches[0];
            queuedBranches.RemoveAt(0);

            WriteQueuedBranches(queuedBranches, filePath);

            return dequeueBranch;
        }

        internal static void RemoveBranch(string repository, string branchId, string filePath)
        {
            List<Branch> queuedBranches = GetQueuedBranches(filePath);

            int index = BranchFinder.IndexOf(queuedBranches, repository, branchId);
            if (index == -1)
                return;

            queuedBranches.RemoveAt(index);
            WriteQueuedBranches(queuedBranches, filePath);
        }

        internal static void WriteQueuedBranches(IEnumerable<Branch> branches, string filePath)
        {
            if (branches == null)
                return;

            try
            {
                using (StreamWriter file = new StreamWriter(filePath))
                {
                    foreach (Branch branch in branches)
                        file.WriteLine(BranchParser.ToString(branch));
                }
            }
            catch (Exception ex)
            {
                LogException("Error writing the queued branches to '{0}': {1}", ex, filePath);
            }
        }

        static List<Branch> GetQueuedBranches(string filePath)
        {
            List<Branch> result = new List<Branch>();

            try
            {
                if (!File.Exists(filePath))
                    return result;

                foreach (string line in File.ReadAllLines(filePath))
                {
                    Branch branch;
                    if (!BranchParser.TryParse(line, out branch))
                    {
                        mLog.ErrorFormat("Malformed line while reading branches file: {0}", line);
                        continue;
                    }
                    result.Add(branch);
                }
            }
            catch (Exception ex)
            {
                LogException("Error reading the queued branches to '{0}': {1}", ex, filePath);
            }

            return result;
        }

        static void LogException(string message, Exception e, params string[] args)
        {
            mLog.ErrorFormat(message, args, e.Message);
            mLog.DebugFormat("StackTrace:{0}{1}", Environment.NewLine, e.StackTrace);
        }

        static class BranchParser
        {
            internal static bool TryParse(string line, out Branch branch)
            {
                branch = null;

                if (string.IsNullOrEmpty(line))
                    return false;

                string[] fields = ReadFieldsWithFinalComment(line, 5, SEPARATOR);
                if (fields == null)
                    return false;

                branch = new Branch(fields[0], fields[1], fields[2], fields[3], fields[4]);
                return true;
            }

            internal static string ToString(Branch branch)
            {
                string comment = branch.Comment.
                    Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ").Trim();

                return string.Format("{0}|{1}|{2}|{3}|{4}",
                    branch.Repository, branch.Id, branch.FullName, branch.Owner, comment);
            }

            static string[] ReadFieldsWithFinalComment(
                string line, int count, string separator)
            {
                string pendingToProcess = line;

                string[] result = new string[count];

                for (int i = 0; i < count - 1; i++)
                {
                    string field = GetNextField(ref pendingToProcess, separator);
                    if (field == null)
                        return null;

                    result[i] = field;
                }

                result[count - 1] = pendingToProcess;
                return result;
            }

            static string GetNextField(ref string contentToProcess, string separator)
            {
                int index = contentToProcess.IndexOf(separator);
                if (index == -1)
                {
                    contentToProcess = string.Empty;
                    return null;
                }

                string result = contentToProcess.Substring(0, index);
                if (index == contentToProcess.Length)
                    contentToProcess = string.Empty;
                else
                    contentToProcess = contentToProcess.Substring(index + 1);

                return result;
            }

            const string SEPARATOR = "|";
        }

        static class BranchFinder
        {
            internal static int IndexOf(
                List<Branch> branches, string repository, string branchId)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (!branches[i].Repository.Equals(repository))
                        continue;

                    if (!branches[i].Id.Equals(branchId))
                        continue;

                    return i;
                }

                return -1;
            }
        }

        static readonly ILog mLog = LogManager.GetLogger("BranchesQueueStorage");
    }
}
