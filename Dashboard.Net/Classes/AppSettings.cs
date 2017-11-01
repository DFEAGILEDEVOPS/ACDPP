using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dashboard;
using System.Configuration;
using Extensions;
using System.Reflection;

namespace Dashboard.Classes
{
    public class AppSettings
    {
        public static string SourceAccountName = ConfigurationManager.AppSettings["SourceAccountName"];
        public static string SourceProjectName = ConfigurationManager.AppSettings["SourceProjectName"];
        public static string SourceRepoName = ConfigurationManager.AppSettings["SourceRepoName"];
        public static string SourceBuildName = ConfigurationManager.AppSettings["SourceBuildName"];

        public static string VSTSAccountEmail = ConfigurationManager.AppSettings["VSTSAccountEmail"];
        public static string VSTSPersonalAccessToken = ConfigurationManager.AppSettings["VSTSPersonalAccessToken"];

        public static string SourceInstanceUrl = $"https://{SourceAccountName}.visualstudio.com/";
        public static string SourceProjectUrl = $"{SourceInstanceUrl}{SourceProjectName}";
        public static string SourceCollectionUrl = $"{SourceInstanceUrl}DefaultCollection";
        public static string SourceRepoUrl = $"{SourceProjectUrl}/_git/{SourceRepoName}";

        public static string TargetProjectTemplateId = ConfigurationManager.AppSettings["TargetProjectTemplateId"];

        public static string ProjectCreatedBy = ConfigurationManager.AppSettings["ProjectCreatedBy"].ToStringOr(Assembly.GetExecutingAssembly().GetName().Name);

        public static string GovNotifyApiKey = ConfigurationManager.AppSettings["GovNotifyApiKey"];
        public static string GovNotifyApiTestKey = ConfigurationManager.AppSettings["GovNotifyApiTestKey"];
        public static string WelcomeTemplateId = ConfigurationManager.AppSettings["WelcomeTemplateId"];
    }
}
