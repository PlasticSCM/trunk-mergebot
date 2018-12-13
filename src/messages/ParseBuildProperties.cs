using System.Collections.Generic;

namespace TrunkBot.Messages
{
    public class BuildProperties
    {
        public string BuildNumber { get; set; }
        public string BuildName { get; set; }
        public string TaskNumber { get; set; }
        public string BranchName { get; set; }
        public string BranchHead { get; set; }
        public string BranchHeadGuid { get; set; }
        public string TrunkHead { get; set; }
        public string TrunkHeadGuid { get; set; }
        public string ReleaseNotes { get; set; }
        public string ChangesetOwner { get; set; }
        public string RepSpec { get; set; }
        public string LabelName { get; set; }
    }

    public static class ParseBuildProperties
    {
        public static Dictionary<string, string> ToDictionary(BuildProperties properties)
        {
            return new Dictionary<string, string>
                {
                    { PropertyKey.BuildNumber, properties.BuildNumber },
                    { PropertyKey.BuildName, properties.BuildName },
                    { PropertyKey.TaskNumber, properties.TaskNumber },
                    { PropertyKey.BranchName, properties.BranchName },
                    { PropertyKey.BranchHead, properties.BranchHead },
                    { PropertyKey.BranchHeadGuid, properties.BranchHeadGuid },
                    { PropertyKey.TrunkHead, properties.TrunkHead },
                    { PropertyKey.TrunkHeadGuid, properties.TrunkHeadGuid },
                    { PropertyKey.ReleaseNotes, properties.ReleaseNotes },
                    { PropertyKey.ChangesetOwner, properties.ChangesetOwner },
                    { PropertyKey.RepSpec, properties.RepSpec },
                    { PropertyKey.LabelName, properties.LabelName },
                };
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
        }
    }  
}
