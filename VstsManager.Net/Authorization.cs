using System.Collections.Generic;

namespace VstsApi.Net
{

        public class Authorization
        {
            public string Scheme { get; set; }
            public Dictionary<string, string> Parameters { get; set; }
        }
}