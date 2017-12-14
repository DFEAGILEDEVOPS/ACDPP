using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Director.NetStandard
{
    public class Director
    {
        public Director(string scriptContent, string mimeType)
        {
            ScriptContent = scriptContent;
            MimeType = mimeType;
        }
        private readonly string ScriptContent;
        private readonly string MimeType;

        public List<Object> Parse()
        {
            var items = JsonConvert.DeserializeObject<List<Object>>(ScriptContent);


        }


    }
}
