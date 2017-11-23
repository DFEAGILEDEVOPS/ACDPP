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
        #region Sql KeyVault
        public static IVault GetKeyVault(string id, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var vault = azure.Vaults.GetById(id);
            return vault;
        }

        public static IEnumerable<IVault> ListKeyVaults(string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            var vaults = azure.Vaults.ListByResourceGroup(resourceGroup);
            return vaults;
        }

        public static IVault CreateKeyVault(string vaultName, string resourceGroup, Region region=null, IAzure azure=null)
        {
            if (string.IsNullOrWhiteSpace(vaultName)) vaultName = SdkContext.RandomResourceName("ACDPP-VAULT", 24);

            if (azure == null) azure = Core.Authenticate();

            if (region == null) region = Core.GetResourceGroup(resourceGroup, azure)?.Region;

            var vault = azure.Vaults.Define(vaultName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithEmptyAccessPolicy()
                .Create();

            return vault;
        }

        public static void DeleteKeyVault(string vaultName, string resourceGroup, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.Vaults.DeleteByResourceGroup(resourceGroup, vaultName);
        }

        public static void DeleteKeyVault(IVault vault, IAzure azure = null)
        {
            if (azure == null) azure = Core.Authenticate();
            azure.Vaults.DeleteById(vault.Id);
        }

        #endregion

    }
}
