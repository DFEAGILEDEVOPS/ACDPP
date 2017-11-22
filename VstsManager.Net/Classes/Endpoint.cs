namespace VstsApi.Net.Classes
{
    public class Endpoint 
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public Authorization Authorization { get; set; }
        public string IsReady { get; set; }
    }
}