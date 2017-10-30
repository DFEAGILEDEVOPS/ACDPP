using Dashboard.Classes;
using static VstsApi.Net.VstsManager;

namespace Dashboard.Net.Models
{
    public class TeamMemberViewModel : UserViewModel
    {
        public string TeamMemberId { get; set; }
        public LicenceTypes LicenceType
        {
            get
            {
                switch (Role)
                {
                    case RoleTypes.Developer:
                    case RoleTypes.Releaser:
                        return LicenceTypes.Basic;
                    default:
                        return LicenceTypes.Stakeholder;
                }
            }
        }
    }
}