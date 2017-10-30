using Dashboard.Classes;
using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard.Net.Models
{
    public class UserViewModel
    {
        [Required(AllowEmptyStrings = false)]
        public string FirstName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string LastName { get; set; }
        public string Name => $"{FirstName} {LastName}".TrimI();
    
        [EmailAddress]
        [Required(AllowEmptyStrings = false)]
        public string Email { get; set; }

        public RoleTypes Role { get; set; } = RoleTypes.Stakeholder;


    }
}
