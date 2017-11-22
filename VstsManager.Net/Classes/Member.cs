namespace VstsApi.Net.Classes
{
    public class Member
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string EmailAddress { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public RoleTypes Role { get; set; } = RoleTypes.Developer;
        public LicenceTypes LicenceType => Role.ToLicenceType();
    }
}