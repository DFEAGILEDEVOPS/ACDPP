using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class CopyBuildModel
    {
        public string SourceProjectId { get; set; }
        public string SourceBuildName { get; set; }
        public string TargetProjectId { get; set; }
        public string TargetBuildName { get; set; }
        public string DefinitionId { get; set; }

        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Secrets { get; set; } = new Dictionary<string, string>();
        public object[] TargetRepoName { get; internal set; }
    }
}
