using VstsApi.Net.Classes;
namespace Dashboard.Net.Models
{
    public class TeamMemberViewModel : UserViewModel
    {
        public string TeamMemberId { get; set; }
        public LicenceTypes LicenceType => Role.ToLicenceType();
    }
}