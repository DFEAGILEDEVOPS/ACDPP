using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class DeleteKeyVaultModel
    {
        public string ProjectId { get; set; }
        public string GroupName { get; set; }
        public string VaultName { get; set; }
    }
}
