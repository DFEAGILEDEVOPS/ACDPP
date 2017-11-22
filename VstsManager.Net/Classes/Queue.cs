namespace VstsApi.Net.Classes
{
    public class Queue
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public QueuePool Pool { get; set; }
        public string GroupScopeId { get; set; }
    }
}