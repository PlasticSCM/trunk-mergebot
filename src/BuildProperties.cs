using System.Collections.Generic;

namespace TrunkBot
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
        public string Stage { get; set; }
        public Dictionary<string, string> UserDefinedBranchAttributes { get; set; }

        public static class StageValues
        {
            public const string PRE_CHECKIN = "pre";
            public const string POST_CHECKIN = "post";
        }
    }
}
