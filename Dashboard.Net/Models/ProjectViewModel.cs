using Dashboard.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;

namespace Dashboard.Net.Models
{
    public class ProjectViewModel
    {
        public ProjectViewModel()
        {
        }

        public string Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string CostCode { get; set; }

        public List<TeamMemberViewModel> TeamMembers { get; set; } = new List<TeamMemberViewModel>();

        public NameValueCollection Properties{ get; set; }
    }
}