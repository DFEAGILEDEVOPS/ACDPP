using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class SaveKeyVaultModel
    {
        public string ProjectId { get; set; }
        public string GroupName { get; set; }
        public string VaultName { get; set; }
        public List<string> AppIds { get; set; }
        public string VaultId { get; internal set; }
        public string VaultUri { get; internal set; }
    }
}
