using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Extensions;
namespace Dashboard.Net.Models
{
    public class TeamMemberViewModel : UserViewModel
    {
        public string TeamMemberId { get; set; }
    }
}