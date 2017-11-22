using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Linq;
using System;

namespace AzureApi.Net
{

    public class ActiveDirectoryHelper
    {
        #region App Registrations
        public static IApplication GetAppRegistration(string appName, ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = AuthenticationHelper.GetActiveDirectoryClientAsApplication();

            var appRegistrations = activeDirectoryClient.Applications.Where(a=>a.DisplayName.ToLower()==appName.ToLower()).ExecuteAsync().Result;
            var appRegistration= appRegistrations==null ? null : appRegistrations.CurrentPage.ToList().FirstOrDefault();
            return appRegistration;
        }

        public static IEnumerable<IApplication> ListAppRegistrations(ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = AuthenticationHelper.GetActiveDirectoryClientAsApplication();

            var results = new List<IApplication>();
            var appRegistrations = activeDirectoryClient.Applications.ExecuteAsync().Result;

            do
            {
                if (appRegistrations != null && appRegistrations.CurrentPage != null && appRegistrations.CurrentPage.Count > 0)
                    results.AddRange(appRegistrations.CurrentPage);

                if (!appRegistrations.MorePagesAvailable) break;

                appRegistrations = appRegistrations.GetNextPageAsync().Result;
            }
            while (true);

            return results;
        }

        public static IApplication CreateAppRegistration(string appName, string url, ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = AuthenticationHelper.GetActiveDirectoryClientAsApplication();

            var application = new Application { DisplayName = appName };
            application.IdentifierUris.Add(url);
            application.ReplyUrls.Add(url);

            AppRole appRole = new AppRole();
            appRole.Id = Guid.NewGuid();
            appRole.IsEnabled = true;
            appRole.AllowedMemberTypes.Add("User");
            appRole.DisplayName = "Something";
            appRole.Description = "Anything";
            appRole.Value = "policy.write";
            application.AppRoles.Add(appRole);

            // created Keycredential object for the new App object
            KeyCredential keyCredential = new KeyCredential
            {
                
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1),
                Type = "Symmetric",
                Value = Convert.FromBase64String("g/TMLuxgzurjQ0Sal9wFEzpaX/sI0vBP3IBUE/H/NS4="),
                Usage = "Verify"
            };
            application.KeyCredentials.Add(keyCredential);
            activeDirectoryClient.Applications.AddApplicationAsync(application).Wait();

            return application;
        }

        public static void DeleteAppRegistration(IApplication application)
        {
            application.DeleteAsync().Wait();
        }
        #endregion

    }
}
