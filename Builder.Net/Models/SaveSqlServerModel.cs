using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class SaveSqlServerModel
    {
        public string ProjectId { get; set; }
        public string GroupName { get; set; }
        public string AdministratorLogin { get; set; }
        public string AdministratorPassword { get; set; }
        public HashSet<string> FirewallRules { get; set; }
        public bool AllowAzureAccess { get; set; }
        public string ServerName { get; set; }
    }
}
