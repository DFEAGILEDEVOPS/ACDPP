using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class DeleteSqlDatabaseModel
    {
        public string ProjectId { get; set; }
        public string GroupName { get; set; }
        public string ServerName { get; internal set; }
        public string DatabaseName { get; set; }
    }
}
