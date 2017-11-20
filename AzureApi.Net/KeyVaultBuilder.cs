using Microsoft.Azure.Management;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Fluent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;

namespace AzureApi.Net
{

    public class KeyVaultBuilder
    {
        public static string VaultUrl = ConfigurationManager.AppSettings["VaultUrl"];
        public static string VaultClientId = ConfigurationManager.AppSettings["VaultClientId"];
        public static string VaultClientSecret = ConfigurationManager.AppSettings["VaultClientSecret"];

        #region Sql KeyVault
        internal static IVault GetKeyVault(string id, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var vault = azure.Vaults.GetById(id);
            return vault;
        }

        internal static IEnumerable<IVault> ListKeyVaults(string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var vaults = azure.Vaults.ListByResourceGroup(resourceGroup);
            return vaults;
        }

        internal static IVault CreateKeyVault(string vaultName, string resourceGroup, string adminUsername, string adminPassword, Region region=null, IAzure azure=null)
        {
            if (string.IsNullOrWhiteSpace(vaultName)) vaultName = SdkContext.RandomResourceName("ACDPP-VAULT", 20);

            if (azure == null) azure = Core.Authenticate();

            if (region == null) region = Core.GetResourceGroup(resourceGroup, azure)?.Region;

            var vault = azure.Vaults.Define(vaultName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithEmptyAccessPolicy()
                .Create();

            return vault;
        }
      
        internal static void DeleteKeyVault(string vaultName, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.Vaults.DeleteByResourceGroup(resourceGroup, vaultName);
        }

        internal static void DeleteKeyVault(IVault vault, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.Vaults.DeleteById(vault.Id);
        }

        #endregion

    }
}
