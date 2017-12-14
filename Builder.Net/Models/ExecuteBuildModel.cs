using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class ExecuteBuildModel
    {
        public string ProjectId { get; internal set; }
        public string BuildName { get; internal set; }
        public int RunSeconds { get; internal set; } = 300;
        public int StartSeconds { get; internal set; } = 300;
        public bool OnceOnly { get; internal set; }
        public string BuildId { get; internal set; }
    }
}
