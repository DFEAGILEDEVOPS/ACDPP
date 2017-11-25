using Microsoft.Azure.Management;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.KeyVault;
using Microsoft.Azure.Management.KeyVault.Fluent;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Configuration;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace AzureApi.Net
{

    public class KeyVaultBuilder
    {
        class AccessPolicy : IAccessPolicy
        {
            public string TenantId {get;set;}

            public string ObjectId { get; set; }

            public string ApplicationId { get; set; }

            public Permissions Permissions { get; set; }

            public string Name { get; set; }

            public string Key { get; set; }

            public IVault Parent { get; set; }

            public AccessPolicyEntry Inner { get; set; }
        }

        #region Sql KeyVault
        public static IVault GetKeyVault(IAzure azure, string id, string resourceGroup)
        {
            var vault = azure.Vaults.GetById(id);
            return vault;
        }

        public static IEnumerable<IVault> ListKeyVaults(IAzure azure, string resourceGroup)
        {
            var vaults = azure.Vaults.ListByResourceGroup(resourceGroup);
            return vaults;
        }

        public static IVault CreateKeyVault(IAzure azure, string ownerClientId, string vaultName, string resourceGroup, Region region=null)
        {
            if (string.IsNullOrWhiteSpace(vaultName)) vaultName = SdkContext.RandomResourceName("ACDPP-VAULT", 24);

            if (region == null) region = Core.GetResourceGroup(azure, resourceGroup)?.Region;
            
            var vault = azure.Vaults.Define(vaultName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .DefineAccessPolicy()
                        .ForServicePrincipal(ownerClientId)
                        .AllowKeyAllPermissions().AllowSecretAllPermissions()
                        .Attach()
                .Create();
            
            
            return vault;
        }

        public static void AddAccessPolicy(IVault vault, string clientId)
        {
            vault.Update().DefineAccessPolicy().ForServicePrincipal(clientId).AllowKeyAllPermissions().AllowSecretAllPermissions().Attach().Apply();
        }

        public static void DeleteKeyVault(IAzure azure, string vaultName, string resourceGroup)
        {
            azure.Vaults.DeleteByResourceGroup(resourceGroup, vaultName);
        }

        public static void DeleteKeyVault(IAzure azure, IVault vault)
        {
            azure.Vaults.DeleteById(vault.Id);
        }

        #endregion

    }
}
