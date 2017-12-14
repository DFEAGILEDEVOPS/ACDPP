using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class CopyReleaseModel
    {
        public string SourceProjectId { get; set; }
        public string SourceReleaseName { get; set; }
        public string TargetProjectId { get; set; }
        public string TargetReleaseName { get; set; }
        public string ReleaseId { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Secrets { get; set; } = new Dictionary<string, string>();
        public object[] TargetRepoName { get; internal set; }
    }
}
