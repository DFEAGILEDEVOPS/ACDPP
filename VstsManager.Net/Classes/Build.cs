namespace VstsApi.Net.Classes
{

    public class Build
        {
            public string Id { get; set; }
            public string Url { get; set; }
            public Definition Definition { get; set; }
            public string SourceBranch { get; set; }
            public string Parameters { get; set; }
            public string QueueTime { get; set; }
            public string StartTime { get; set; }
            public string FinishTime { get; set; }
            public string Status { get; set; }
            public string Result { get; set; }
            public string Reason { get; set; }
        }
}