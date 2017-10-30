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
    internal class AppSettings
    {
        public static string ProjectCreatedBy = ConfigurationManager.AppSettings["ProjectCreatedBy"].ToStringOr(Assembly.GetExecutingAssembly().GetName().Name);

        public static string CurrentUserEmail = ConfigurationManager.AppSettings["CurrentUserEmail"];
        public static string VSTSPersonalAccessToken = ConfigurationManager.AppSettings["VSTSPersonalAccessToken"];
        public static string SourceRepoUsername = ConfigurationManager.AppSettings["SourceRepoUsername"];
        public static string SourceRepoPassword = ConfigurationManager.AppSettings["SourceRepoPassword"];
        public static string SourceRepoUrl = ConfigurationManager.AppSettings["SourceRepoUrl"];

        public static string GovNotifyApiKey = ConfigurationManager.AppSettings["GovNotifyApiKey"];
        public static string GovNotifyApiTestKey = ConfigurationManager.AppSettings["GovNotifyApiTestKey"];
        public static string WelcomeTemplateId = ConfigurationManager.AppSettings["WelcomeTemplateId"];
        public static string SmtpServer = ConfigurationManager.AppSettings["SmtpServer"];
        public static string SmtpUsername = ConfigurationManager.AppSettings["SmtpUsername"];
        public static string SmtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
    }
}
