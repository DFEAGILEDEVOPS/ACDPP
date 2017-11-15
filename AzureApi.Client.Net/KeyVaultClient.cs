using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using System.Threading.Tasks;

namespace AzureApi.Client.Net
{
    public class VaultClient
    {
        public static string VaultUrl = ConfigurationManager.AppSettings["VaultUrl"];
        public static string VaultClientId = ConfigurationManager.AppSettings["VaultClientId"];
        public static string VaultClientSecret = ConfigurationManager.AppSettings["VaultClientSecret"];

        public VaultClient(string vaultUrl,string vaultClientId,string vaultClientSecret)
        {
            VaultUrl=vaultUrl;
            VaultClientId=vaultClientId;
            VaultClientSecret=vaultClientSecret;
        }

        public static string GetSecret(string key)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync),new System.Net.Http.HttpClient());
            
            var secret = Task.Run(async ()=>await client.GetSecretAsync(VaultUrl, key));
            
            return secret?.Result?.Value;
        }

        public static string SetSecret(string key, string value)
        {
            var client = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync), new System.Net.Http.HttpClient());

            var secret = Task.Run(async () => await client.SetSecretAsync(VaultUrl, key, value));

            return secret?.Result?.Id;
        }

        private static async Task<string> GetAccessTokenAsync(string authority,string resource,string scope)
        {
            Microsoft.Azure.c
            //clientID and clientSecret are obtained by registering
            //the application in Azure AD
            var clientCredential = new ClientCredential(VaultClientId,VaultClientSecret);

            var context = new AuthenticationContext(authority,TokenCache.DefaultShared);

            var result = await context.AcquireTokenAsync(resource,clientCredential);

            return result.AccessToken;
        }


    }
}
