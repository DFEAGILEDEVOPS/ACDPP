using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dashboard;

namespace Dashboard.Classes
{
    public class AppSettings
    {
        public static string VSTSPersonalAccessToken = Startup.Configuration["VSTSPersonalAccessToken"];
        public static string SourceRepoUsername = Startup.Configuration["SourceRepoUsername"];
        public static string SourceRepoPassword = Startup.Configuration["SourceRepoPassword"];
        public static string SourceRepoUrl = Startup.Configuration["SourceRepoUrl"];
    }
}
