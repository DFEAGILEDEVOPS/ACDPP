using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VstsApi.Net.Classes;

namespace Builder.Net
{
    public class SaveTeamMembersModel
    {
        public string ProjectId { get; set; }
        public List<Member> Members { get; set; }
    }
}
