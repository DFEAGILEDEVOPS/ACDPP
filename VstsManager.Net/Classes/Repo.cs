namespace VstsApi.Net.Classes
{
    public class Repo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DefaultBranch { get; set; }
        public string Url { get; set; }
        public string RemoteUrl { get; set; }
        public Project Project { get; set; }
    }
}