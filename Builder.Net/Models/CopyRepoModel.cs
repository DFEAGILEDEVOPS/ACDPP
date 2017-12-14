using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class CopyRepoModel
    {
        public string SourceProjectId { get; internal set; }
        public string SourceRepoName { get; internal set; }
        public string TargetProjectId { get; internal set; }
        public string TargetRepoName { get; internal set; }
        public string TargetUrl { get; set; }
    }
}
