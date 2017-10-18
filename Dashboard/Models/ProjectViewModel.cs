using Dashboard.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public class ProjectViewModel
    {
        [Required(AllowEmptyStrings = false)]
        public string CostCode { get; set; }
        [Required(AllowEmptyStrings = false)]
        public string TeamProjectName { get; set; }
        public List<TeamMember> TeamMembers { get; set; }
        public TeamMember[] TeamMembersArray { get; set; }

        public class TeamMember
        {
            [Required(AllowEmptyStrings =false)]
            public string Name { get; set; }
            [EmailAddress]
            [Required(AllowEmptyStrings = false)]
            public string Email { get; set; }
        }
    }
}
