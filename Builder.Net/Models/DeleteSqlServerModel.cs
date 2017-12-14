using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class DeleteSqlServerModel
    {
        public string SourceProjectId { get; set; }
        public string GroupName { get; set; }
        public string ProjectId { get; internal set; }
        public string ServerName { get; internal set; }
    }
}
