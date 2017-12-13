using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VstsApi.Net.Classes;

namespace Builder.Net
{
    public class CreateResourceGroupModel
    {
        public string ProjectId { get; set; }
        public string GroupName { get; set; }
        public string Region { get; set; }
        public string GroupId { get; set; }
    }
}
