using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System.Collections.Generic;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Text;

namespace AzureApi.Net
{

    public class ActiveDirectoryHelper
    {
        public ActiveDirectoryHelper(string clientId, string clientSecret, string azureTenantId)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            AzureTenantId = azureTenantId;
            AuthorityUrl = $"https://login.windows.net/{azureTenantId}";
        }
        readonly string AzureTenantId;
        readonly string AuthorityUrl;
        readonly string ClientId;
        readonly string ClientSecret;

        const string ResourceUrl = "https://graph.windows.net";

        #region Authentication Helpers

        /// <summary>
        /// Get Active Directory Client for Application.
        /// </summary>
        /// <returns>ActiveDirectoryClient for Application.</returns>
        public ActiveDirectoryClient GetActiveDirectoryClientAsApplication()
        {
            Uri servicePointUri = new Uri(ResourceUrl);
            Uri serviceRoot = new Uri(servicePointUri, AzureTenantId);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot, async () => await AcquireTokenAsyncForApplication());
            return activeDirectoryClient;
        }
        /// <summary>
        /// Async task to acquire token for Application.
        /// </summary>
        /// <returns>Async Token for application.</returns>
        public async Task<string> AcquireTokenAsyncForApplication()
        {
            return GetTokenForApplication();
        }

        /// <summary>
        /// Get Token for Application.
        /// </summary>
        /// <returns>Token for application.</returns>
        public string GetTokenForApplication()
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthorityUrl, false);
            ClientCredential clientCred = new ClientCredential(ClientId, ClientSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireToken(ResourceUrl, clientCred);
            return authenticationResult.AccessToken;
        }


        /// <summary>
        /// Get Active Directory Client for User.
        /// </summary>
        /// <returns>ActiveDirectoryClient for User.</returns>
        public ActiveDirectoryClient GetActiveDirectoryClientAsUser()
        {
            Uri servicePointUri = new Uri(ResourceUrl);
            Uri serviceRoot = new Uri(servicePointUri, AzureTenantId);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot, async () => await AcquireTokenAsyncForUser());
            return activeDirectoryClient;
        }
        /// <summary>
        /// Async task to acquire token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public async Task<string> AcquireTokenAsyncForUser()
        {
            return GetTokenForUser();
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public string GetTokenForUser()
        {
            var redirectUri = new Uri("https://localhost");
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthorityUrl, false);
            AuthenticationResult userAuthnResult = authenticationContext.AcquireToken(ResourceUrl, ClientId, redirectUri, PromptBehavior.Always);
            return userAuthnResult.AccessToken;
        }
        #endregion

        #region App Registrations
        public IApplication GetAppRegistration(string appName, ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = GetActiveDirectoryClientAsApplication();

            var appRegistrations = activeDirectoryClient.Applications.Where(a=>a.DisplayName.ToLower()==appName.ToLower()).ExecuteAsync().Result;
            var appRegistration= appRegistrations==null ? null : appRegistrations.CurrentPage.ToList().FirstOrDefault();
            return appRegistration;
        }

        public IEnumerable<IApplication> ListAppRegistrations(ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = GetActiveDirectoryClientAsApplication();

            var results = new List<IApplication>();
            var appRegistrations = activeDirectoryClient.Applications.Take(100).ExecuteAsync().Result;

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

        public IApplication CreateAppRegistration(string appName, string url, ActiveDirectoryClient activeDirectoryClient = null)
        {
            if (activeDirectoryClient == null) activeDirectoryClient = GetActiveDirectoryClientAsApplication();

            //Create the application
            var application = new Application { DisplayName = appName };
            application.IdentifierUris.Add(url);
            application.ReplyUrls.Add(url);

            activeDirectoryClient.Applications.AddApplicationAsync(application).Wait();

            //Create the service principal
            var newServicePrincipal = new ServicePrincipal
            {
                DisplayName = application.DisplayName,
                AccountEnabled = true,
                AppId = application.AppId
            };
            activeDirectoryClient.ServicePrincipals.AddServicePrincipalAsync(newServicePrincipal).Wait();
            return application;
        }

        public void DeleteAppRegistration(IApplication application)
        {
            application.DeleteAsync().Wait();
        }

        public void AddApplicationResourcePermissions(IApplication application, string resourceAppId, Dictionary<Guid, string> resourcePermissions)
        {
            var resourceAccess = application.RequiredResourceAccess.FirstOrDefault(r => r.ResourceAppId == resourceAppId);

            var permissions = resourcePermissions.Select(kvp => new ResourceAccess() { Id = kvp.Key, Type = kvp.Value }).ToList();
            if (resourceAccess != null)
            {
                resourceAccess.ResourceAccess = permissions;
            }
            else
            {
                resourceAccess = new RequiredResourceAccess() { ResourceAppId = resourceAppId, ResourceAccess=permissions };
                application.RequiredResourceAccess.Add(resourceAccess);
            }
            application.UpdateAsync().Wait();
        }

        public string AddApplicationPassword(IApplication application, string keyName, string keyValue, DateTime? endDate = null)
        {
            var startDate = DateTime.UtcNow;
            if (endDate == null || endDate < startDate) endDate = DateTime.UtcNow.AddYears(1);
            var value = Convert.ToBase64String(Encoding.UTF8.GetBytes(keyValue));

            var passwordCredential = application.PasswordCredentials.FirstOrDefault(p => Encoding.UTF8.GetString(p.CustomKeyIdentifier).ToLower() == keyName.ToLower());
            if (passwordCredential != null)
            {
                passwordCredential.Value = value;
                passwordCredential.KeyId = null;
            }
            else
            {
                passwordCredential = new PasswordCredential
                {
                    StartDate = startDate,
                    EndDate = endDate.Value,
                    CustomKeyIdentifier = Encoding.UTF8.GetBytes(keyName),
                    Value = value
                };
                application.PasswordCredentials.Add(passwordCredential);
            }

            application.UpdateAsync().Wait();
            return value;
        }
        #endregion

    }
}
