﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Builder.Net
{
    public class CopyBuildModel
    {
        public string GroupName { get; set; }
        public string ProjectId { get; internal set; }
        public string RepoName { get; internal set; }
        public string Url { get; set; }
    }
}