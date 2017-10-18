using Dashboard.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Models
{
    public class ProjectViewModel
    {
        public string CostCode { get; set; }
        public string TeamProjectName { get; set; }
        public string TeamProductDescription { get; set; }
        public string TeamTemplate { get; set; }
        public int TeamMemberCount { get; set; }
        public List<TeamMember> TeamMembers { get; set; }
        public TeamMember[] TeamMembersArray { get; set; }

        public class TeamMember
        {
            public string Name { get; set; }
            public string Email { get; set; }
            public RoleTypes Role { get; set; }
        }
    }
}
