using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using log4net;

using WebSocketSharp;

using TrunkBot.Api;
using TrunkBot.Configuration;
using TrunkBot.Messages;
using TrunkBot.WebSockets;

namespace TrunkBot
{
    internal class TrunkMergebot
    {
        internal TrunkMergebot(
            string restApiUrl,
            TrunkBotConfiguration trunkBotConfig,
            string branchesQueueFilePath,
            string botName)
        {
            mTrunkBotConfig = trunkBotConfig;
            mBranchesQueueFilePath = branchesQueueFilePath;
            mBotName = botName;

            mRestApi = new RestApi(restApiUrl, mTrunkBotConfig.UserApiKey);
        }

        internal void LoadBranchesToProcess()
        {
            mLog.Info("Get branches to process...");

            List<Branch> branches = FindQueries.FindResolvedBranches(
                mRestApi,
                mTrunkBotConfig.Repository,
                mTrunkBotConfig.BranchPrefix ?? string.Empty,
                mTrunkBotConfig.Plastic.StatusAttribute.Name,
                mTrunkBotConfig.Plastic.StatusAttribute.ResolvedValue);

            BranchesQueueStorage.WriteQueuedBranches(branches, mBranchesQueueFilePath);
        }

        internal void OnAttributeChanged(object state)
        {
            //No new events are received while this event is processed so avoid process it here

            string message = (string)state;

            mLog.Debug(message);

            BranchAttributeChangeEvent e = ParseEvent.Parse(message);

            if (!ShouldBeProcessed(e, mTrunkBotConfig))
                return;

            if (!IsRelevantAttribute(
                    e.AttributeName, mTrunkBotConfig.Plastic.StatusAttribute))
                return;

            lock (mSyncLock)
            {
                if (!IsAttributeValueResolved(
                        e.AttributeValue, mTrunkBotConfig.Plastic.StatusAttribute))
                {
                    BranchesQueueStorage.RemoveBranch(
                        e.Repository, e.BranchId, mBranchesQueueFilePath);
                    return;
                }

                if (BranchesQueueStorage.Contains(
                        e.Repository, e.BranchId, mBranchesQueueFilePath))
                    return;

                BranchesQueueStorage.EnqueueBranch(
                    new Branch(e.Repository, e.BranchId, e.BranchFullName, e.BranchOwner, e.BranchComment),
                    mBranchesQueueFilePath);

                Monitor.Pulse(mSyncLock);
            }
        }

        internal void ProcessBranches(object state)
        {
            while (true)
            {
                Branch branch;
                lock (mSyncLock)
                {
                    if (!BranchesQueueStorage.HasQueuedBranches(mBranchesQueueFilePath))
                    {
                        Monitor.Wait(mSyncLock, 1000);
                        continue;
                    }

                    branch = BranchesQueueStorage.DequeueBranch(mBranchesQueueFilePath);
                    branch.FullName = FindQueries.GetBranchName(
                        mRestApi, branch.Repository, branch.Id);
                }

                mLog.InfoFormat("Processing branch {0} attribute change...", branch.FullName);
                ProcessBranch.Result result = ProcessBranch.TryProcessBranch(
                    mRestApi, branch, mTrunkBotConfig, mBotName);

                if (result == ProcessBranch.Result.Ok)
                {
                    mLog.InfoFormat("Branch {0} process completed.", branch.FullName);
                    continue;
                }

                if (result == ProcessBranch.Result.Failed)
                {
                    mLog.InfoFormat("Branch {0} process failed.", branch.FullName);
                    continue;
                }

                mLog.InfoFormat("Branch {0} is not ready. It will be queued again.", branch.FullName);

                lock (mSyncLock)
                {
                    if (BranchesQueueStorage.Contains(
                            branch.Repository, branch.Id,
                            mBranchesQueueFilePath))
                        continue;

                    BranchesQueueStorage.EnqueueBranch(
                        branch, mBranchesQueueFilePath);
                }

                Thread.Sleep(5000);
            }
        }

        static bool ShouldBeProcessed(
            BranchAttributeChangeEvent e,
            TrunkBotConfiguration botConfig)
        {
            if (!e.Repository.Equals(botConfig.Repository,
                    StringComparison.InvariantCultureIgnoreCase))
                return false;

            if (string.IsNullOrEmpty(botConfig.BranchPrefix))
                return true;

            string branchName = BranchSpec.GetName(e.BranchFullName);

            return branchName.StartsWith(botConfig.BranchPrefix,
                StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsRelevantAttribute(
            string attributeName,
            TrunkBotConfiguration.StatusProperty statusAttribute)
        {
            return attributeName.Equals(statusAttribute.Name,
                StringComparison.InvariantCultureIgnoreCase);
        }

        static bool IsAttributeValueResolved(
            string attributeValue,
            TrunkBotConfiguration.StatusProperty statusAttribute)
        {
            return attributeValue.Equals(statusAttribute.ResolvedValue,
                StringComparison.InvariantCultureIgnoreCase);
        }

        readonly object mSyncLock = new object();

        readonly TrunkBotConfiguration mTrunkBotConfig;
        readonly string mBranchesQueueFilePath;
        readonly string mBotName;
        readonly RestApi mRestApi;

        static readonly ILog mLog = LogManager.GetLogger("trunkbot");
    }
}
