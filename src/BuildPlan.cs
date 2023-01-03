using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Codice.CM.Server.Devops;
using Codice.LogWrapper;

namespace TrunkBot
{
    class BuildPlan
    {
        internal class PlanResult
        {
            internal bool Succeeded;
            internal string Explanation;

            internal static PlanResult Cancelled = new PlanResult()
            {
                Succeeded = false,
                Explanation = "The operation was canceled"
            };
        }

        internal static async Task<PlanResult> Build(
            IContinuousIntegrationPlugService ci,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            TrunkMergebot.BuildInProgress buildInProgress,
            string ciName,
            string planBranch,
            string scmSpecToSwitchTo,
            string comment,
            BuildProperties properties,
            CancellationToken cancellationToken)
        {
            return await Run(
                ci,
                storeBuildInProgress,
                buildInProgress,
                ciName,
                planBranch,
                scmSpecToSwitchTo,
                comment,
                ParseBuildProperties.ToDictionary(properties),
                maxWaitTimeSeconds: 4 * 60 * 60,
                cancellationToken);
        }

        internal static async Task<PlanResult> ResumeBuild(
            IContinuousIntegrationPlugService ci,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            string ciName,
            string planBranch,
            string execId,
            string scmSpecToSwitchTo,
            string comment,
            CancellationToken cancellationToken)
        {
            return await WaitForPlanCompletion(
                ci,
                storeBuildInProgress,
                ciName,
                planBranch,
                execId,
                scmSpecToSwitchTo,
                comment,
                maxWaitTimeSeconds: 4 * 60 * 60,
                cancellationToken);
        }

        static async Task<PlanResult> Run(
            IContinuousIntegrationPlugService ci,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            TrunkMergebot.BuildInProgress buildInProgress,
            string ciName,
            string planName,
            string objectSpec,
            string comment,
            Dictionary<string, string> properties,
            int maxWaitTimeSeconds, 
            CancellationToken cancellationToken)
        {
            string execId = await ci.LaunchPlan(ciName, planName, objectSpec, comment, properties);

            buildInProgress.BuildId = execId;

            if (storeBuildInProgress != null)
                storeBuildInProgress.Save(buildInProgress);

            return await WaitForPlanCompletion(
                ci,
                storeBuildInProgress,
                ciName,
                planName,
                execId,
                objectSpec,
                comment,
                maxWaitTimeSeconds,
                cancellationToken);
        }

        static async Task<PlanResult> WaitForPlanCompletion(
            IContinuousIntegrationPlugService ci,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            string ciName,
            string planName,
            string execId,
            string objectSpec,
            string comment,
            int maxWaitTimeSeconds,
            CancellationToken cancellationToken)
            {
            PlanStatus status = await WaitForFinishedPlanStatus(
                        ci,
                storeBuildInProgress,
                ciName,
                planName,
                execId,
                        maxWaitTimeSeconds,
                cancellationToken);

            if (storeBuildInProgress != null && status != PlanStatus.Cancelled)
                storeBuildInProgress.Delete();

            if (status == PlanStatus.Cancelled)
                return PlanResult.Cancelled;

            if (status != null)
            {
                return new PlanResult()
                {
                    Succeeded = status.Succeeded,
                    Explanation = status.Explanation
                };
            }

            return new PlanResult()
            {
                Succeeded = false,
                Explanation = string.Format(
                    "{0} reached the time limit to get the status " +
                    "for plan:'{1}' and executionId:'{2}'" +
                    "\nRequest details: objectSpec:'{3}' and comment:'{4}'",
                    ciName, planName, execId, objectSpec, comment)
            };
        }

        static async Task<PlanStatus> WaitForFinishedPlanStatus(
            IContinuousIntegrationPlugService ci,
            TrunkMergebot.IStoreBuildInProgress storeBuildInProgress,
            string ciName,
            string planBranch,
            string executionId,
            int maxWaitTimeSeconds,
            CancellationToken cancellationToken)
        {
            long startTime = Environment.TickCount;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                    return PlanStatus.Cancelled;

                PlanStatus status = await ci.GetPlanStatus(ciName, planBranch, executionId);

                if (status.IsFinished)
                    return status;

                if (storeBuildInProgress != null && !string.IsNullOrEmpty(status.TranslatedBuildId))
                {
                    TrunkMergebot.BuildInProgress currentBuild = storeBuildInProgress.Load();
                    currentBuild.BuildId = status.TranslatedBuildId;
                    storeBuildInProgress.Save(currentBuild);
                }

                await Task.Delay(5000, cancellationToken);
            } while (Environment.TickCount - startTime < maxWaitTimeSeconds * 1000);

            return new PlanStatus()
            {
                Succeeded = false,
                IsFinished = false,
                Explanation = $"The build reached the max time: {maxWaitTimeSeconds} seconds."
            };
        }

        static readonly ILog mLog = LogManager.GetLogger("TrunkBot-BuildPlan");

        static class ParseBuildProperties
        {
            public static Dictionary<string, string> ToDictionary(BuildProperties properties)
            {
                Dictionary<string, string> result = new Dictionary<string, string>
                {
                    {PropertyKey.BuildNumber, properties.BuildNumber},
                    {PropertyKey.BuildName, properties.BuildName},
                    {PropertyKey.TaskNumber, properties.TaskNumber},
                    {PropertyKey.BranchName, properties.BranchName},
                    {PropertyKey.BranchHead, properties.BranchHead},
                    {PropertyKey.BranchHeadGuid, properties.BranchHeadGuid},
                    {PropertyKey.TrunkHead, properties.TrunkHead},
                    {PropertyKey.TrunkHeadGuid, properties.TrunkHeadGuid},
                    {PropertyKey.ReleaseNotes, properties.ReleaseNotes},
                    {PropertyKey.ChangesetOwner, properties.ChangesetOwner},
                    {PropertyKey.RepSpec, properties.RepSpec},
                    {PropertyKey.LabelName, properties.LabelName},
                    {PropertyKey.Stage, properties.Stage},
                };

                if (properties.UserDefinedBranchAttributes == null)
                {
                    mLog.DebugFormat(
                        "User didn't define any branch attributes to forward for branch '{0}'",
                        properties.BranchName);
                    return result;
                }

                foreach (KeyValuePair<string, string> userDefinedKvp in properties.UserDefinedBranchAttributes)
                {
                    if (result.ContainsKey(userDefinedKvp.Key))
                    {
                        mLog.WarnFormat(
                            "User tried to override attribute '{0}' " +
                            "(original value '{1}', user defined value '{2}'",
                            userDefinedKvp.Key, result[userDefinedKvp.Key], userDefinedKvp.Value);
                        continue;
                    }

                    mLog.DebugFormat(
                        "User defined attribute '{0}' -> '{1}'",
                        userDefinedKvp.Key, userDefinedKvp.Value);

                    result[userDefinedKvp.Key] = userDefinedKvp.Value;
                }

                return result;
            }

            static class PropertyKey
            {
                internal static string BuildNumber = "build.number";
                internal static string BuildName = "build.name";
                internal static string TaskNumber = "task.number";
                internal static string BranchName = "branch.name";
                internal static string BranchHead = "branch.head.changeset.number";
                internal static string BranchHeadGuid = "branch.head.changeset.guid";
                internal static string TrunkHead = "trunk.head.changeset.number";
                internal static string TrunkHeadGuid = "trunk.head.changeset.guid";
                internal static string ReleaseNotes = "release.notes";
                internal static string ChangesetOwner = "branch.head.changeset.author";
                internal static string RepSpec = "repspec";
                internal static string LabelName = "label";
                internal static string Stage = "stage";
            }
        }
    }
}