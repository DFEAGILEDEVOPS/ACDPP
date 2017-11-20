using Microsoft.Azure.KeyVault;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AzureApi.Client.Net
{
    public class VaultClient
    {
        public VaultClient(string vaultUrl, string vaultClientId, string vaultClientSecret,string azureTenantId)
        {
            VaultUrl = vaultUrl;
            VaultClientId = vaultClientId;
            VaultClientSecret = vaultClientSecret;
            AzureTenantId = azureTenantId;
        }

        //Get the json config settings
        public static string VaultUrl;
        public static string VaultClientId;
        public static string VaultClientSecret;
        public static string AzureTenantId;

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

        /*The following method was for using the Microsoft.IdentityModel.Clients.ActiveDirectory library
         * However this library version conflicted with the main app so replaced by the Http method of the same name below */
        //private static async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        //{
        //    //clientID and clientSecret are obtained by registering
        //    //the application in Azure AD
        //    var clientCredential = new ClientCredential(VaultClientId, VaultClientSecret);

        //    var context = new AuthenticationContext(authority, TokenCache.DefaultShared);

        //    var result = await context.AcquireTokenAsync(resource, clientCredential);

        //    return result.AccessToken;
        //}

        private static async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var postData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("resource", resource),
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
                new KeyValuePair<string, string>("client_id", VaultClientId),
                new KeyValuePair<string, string>("client_secret", VaultClientSecret),
            };

            using (var client = new HttpClient())
            {

                string baseUrl = $"https://login.windows.net/{AzureTenantId}/oauth2/";
                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new FormUrlEncodedContent(postData);

                using (var response = await client.PostAsync("token", content))
                {
                    response.EnsureSuccessStatusCode();
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    dynamic authentication = JsonConvert.DeserializeObject(jsonResponse);
                    return authentication.access_token;
                }

                /* successful response
                {
                  "access_token": " eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik5HVEZ2ZEstZnl0aEV1THdqcHdBSk9NOW4tQSJ9.eyJhdWQiOiJodHRwczovL3NlcnZpY2UuY29udG9zby5jb20vIiwiaXNzIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlLyIsImlhdCI6MTM4ODQ0MDg2MywibmJmIjoxMzg4NDQwODYzLCJleHAiOjEzODg0NDQ3NjMsInZlciI6IjEuMCIsInRpZCI6IjdmZTgxNDQ3LWRhNTctNDM4NS1iZWNiLTZkZTU3ZjIxNDc3ZSIsIm9pZCI6IjY4Mzg5YWUyLTYyZmEtNGIxOC05MWZlLTUzZGQxMDlkNzRmNSIsInVwbiI6ImZyYW5rbUBjb250b3NvLmNvbSIsInVuaXF1ZV9uYW1lIjoiZnJhbmttQGNvbnRvc28uY29tIiwic3ViIjoiZGVOcUlqOUlPRTlQV0pXYkhzZnRYdDJFYWJQVmwwQ2o4UUFtZWZSTFY5OCIsImZhbWlseV9uYW1lIjoiTWlsbGVyIiwiZ2l2ZW5fbmFtZSI6IkZyYW5rIiwiYXBwaWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJhcHBpZGFjciI6IjAiLCJzY3AiOiJ1c2VyX2ltcGVyc29uYXRpb24iLCJhY3IiOiIxIn0.JZw8jC0gptZxVC-7l5sFkdnJgP3_tRjeQEPgUn28XctVe3QqmheLZw7QVZDPCyGycDWBaqy7FLpSekET_BftDkewRhyHk9FW_KeEz0ch2c3i08NGNDbr6XYGVayNuSesYk5Aw_p3ICRlUV1bqEwk-Jkzs9EEkQg4hbefqJS6yS1HoV_2EsEhpd_wCQpxK89WPs3hLYZETRJtG5kvCCEOvSHXmDE6eTHGTnEgsIk--UlPe275Dvou4gEAwLofhLDQbMSjnlV5VLsjimNBVcSRFShoxmQwBJR_b2011Y5IuD6St5zPnzruBbZYkGNurQK63TJPWmRd3mbJsGM0mf3CUQ",
                  "token_type": "Bearer",
                  "expires_in": "3600",
                  "expires_on": "1388444763",
                  "resource": "https://service.contoso.com/",
                  "refresh_token": "AwABAAAAvPM1KaPlrEqdFSBzjqfTGAMxZGUTdM0t4B4rTfgV29ghDOHRc2B-C_hHeJaJICqjZ3mY2b_YNqmf9SoAylD1PycGCB90xzZeEDg6oBzOIPfYsbDWNf621pKo2Q3GGTHYlmNfwoc-OlrxK69hkha2CF12azM_NYhgO668yfcUl4VBbiSHZyd1NVZG5QTIOcbObu3qnLutbpadZGAxqjIbMkQ2bQS09fTrjMBtDE3D6kSMIodpCecoANon9b0LATkpitimVCrl-NyfN3oyG4ZCWu18M9-vEou4Sq-1oMDzExgAf61noxzkNiaTecM-Ve5cq6wHqYQjfV9DOz4lbceuYCAA",
                  "scope": "https%3A%2F%2Fgraph.microsoft.com%2Fmail.read",
                  "id_token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJub25lIn0.eyJhdWQiOiIyZDRkMTFhMi1mODE0LTQ2YTctODkwYS0yNzRhNzJhNzMwOWUiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83ZmU4MTQ0Ny1kYTU3LTQzODUtYmVjYi02ZGU1N2YyMTQ3N2UvIiwiaWF0IjoxMzg4NDQwODYzLCJuYmYiOjEzODg0NDA4NjMsImV4cCI6MTM4ODQ0NDc2MywidmVyIjoiMS4wIiwidGlkIjoiN2ZlODE0NDctZGE1Ny00Mzg1LWJlY2ItNmRlNTdmMjE0NzdlIiwib2lkIjoiNjgzODlhZTItNjJmYS00YjE4LTkxZmUtNTNkZDEwOWQ3NGY1IiwidXBuIjoiZnJhbmttQGNvbnRvc28uY29tIiwidW5pcXVlX25hbWUiOiJmcmFua21AY29udG9zby5jb20iLCJzdWIiOiJKV3ZZZENXUGhobHBTMVpzZjd5WVV4U2hVd3RVbTV5elBtd18talgzZkhZIiwiZmFtaWx5X25hbWUiOiJNaWxsZXIiLCJnaXZlbl9uYW1lIjoiRnJhbmsifQ."
                }*/

                /* failed response
                {
                  "error": "invalid_grant",
                  "error_description": "AADSTS70002: Error validating credentials. AADSTS70008: The provided authorization code or refresh token is expired. Send a new interactive authorization request for this user and resource.\r\nTrace ID: 3939d04c-d7ba-42bf-9cb7-1e5854cdce9e\r\nCorrelation ID: a8125194-2dc8-4078-90ba-7b6592a7f231\r\nTimestamp: 2016-04-11 18:00:12Z",
                  "error_codes": [
                    70002,
                    70008
                  ],
                  "timestamp": "2016-04-11 18:00:12Z",
                  "trace_id": "3939d04c-d7ba-42bf-9cb7-1e5854cdce9e",
                  "correlation_id": "a8125194-2dc8-4078-90ba-7b6592a7f231"
                }*/
            }
        }

    }
}
