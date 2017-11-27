using System.Collections.Generic;

namespace VstsApi.Net.Classes
{
    public class Project
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string State { get; set; }
        public string Url { get; set; }
        public Dictionary<string,string> Links { get; set; }
        public string DefaultTeamId { get; set; }
        public Dictionary<string,string> Properties { get; set; }
        public List<Member> Members { get; set; }
        public string CostCode { get; set; }
    }
}