using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System;
using System.Threading.Tasks;

namespace AzureApi.Net
{
    internal class AuthenticationHelper
    {
        public const string TenantName = "GraphDir1.onMicrosoft.com";
        public const string TenantId = "4fd2b2f2-ea27-4fe5-a8f3-7b1a7c975f34";
        public const string ClientId = "118473c2-7619-46e3-a8e4-6da8d5f56e12";
        public const string ClientSecret = "hOrJ0r0TZ4GQ3obp+vk3FZ7JBVP+TX353kNo6QwNq7Q=";
        public const string ClientIdForUserAuthn = "66133929-66a4-4edc-aaee-13b04b03207d";
        public const string AuthString = "https://login.windows.net/" + TenantName;
        public const string ResourceUrl = "https://graph.windows.net";

        public static string TokenForUser;

        /// <summary>
        /// Async task to acquire token for Application.
        /// </summary>
        /// <returns>Async Token for application.</returns>
        public static async Task<string> AcquireTokenAsyncForApplication()
        {
            return GetTokenForApplication();
        }

        /// <summary>
        /// Get Token for Application.
        /// </summary>
        /// <returns>Token for application.</returns>
        public static string GetTokenForApplication()
        {
            AuthenticationContext authenticationContext = new AuthenticationContext(AuthString, false);
            // Config for OAuth client credentials 
            ClientCredential clientCred = new ClientCredential(ClientId, ClientSecret);
            AuthenticationResult authenticationResult = authenticationContext.AcquireToken(ResourceUrl,clientCred);
            string token = authenticationResult.AccessToken;
            return token;
        }

        /// <summary>
        /// Get Active Directory Client for Application.
        /// </summary>
        /// <returns>ActiveDirectoryClient for Application.</returns>
        public static ActiveDirectoryClient GetActiveDirectoryClientAsApplication()
        {
            Uri servicePointUri = new Uri(ResourceUrl);
            Uri serviceRoot = new Uri(servicePointUri, TenantId);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,async () => await AcquireTokenAsyncForApplication());
            return activeDirectoryClient;
        }

        /// <summary>
        /// Async task to acquire token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static async Task<string> AcquireTokenAsyncForUser()
        {
            return GetTokenForUser();
        }

        /// <summary>
        /// Get Token for User.
        /// </summary>
        /// <returns>Token for user.</returns>
        public static string GetTokenForUser()
        {
            if (TokenForUser == null)
            {
                var redirectUri = new Uri("https://localhost");
                AuthenticationContext authenticationContext = new AuthenticationContext(AuthString, false);
                AuthenticationResult userAuthnResult = authenticationContext.AcquireToken(ResourceUrl, ClientIdForUserAuthn, redirectUri, PromptBehavior.Always);
                TokenForUser = userAuthnResult.AccessToken;
                Console.WriteLine("\n Welcome " + userAuthnResult.UserInfo.GivenName + " " + userAuthnResult.UserInfo.FamilyName);
            }
            return TokenForUser;
        }

        /// <summary>
        /// Get Active Directory Client for User.
        /// </summary>
        /// <returns>ActiveDirectoryClient for User.</returns>
        public static ActiveDirectoryClient GetActiveDirectoryClientAsUser()
        {
            Uri servicePointUri = new Uri(ResourceUrl);
            Uri serviceRoot = new Uri(servicePointUri, TenantId);
            ActiveDirectoryClient activeDirectoryClient = new ActiveDirectoryClient(serviceRoot,async () => await AcquireTokenAsyncForUser());
            return activeDirectoryClient;
        }
    }
}