using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.Azure.Management.Storage.Fluent;
using System;
using System.Collections.Generic;

namespace AzureApi.Net
{

    public class StorageBuilder
    {
        #region Storage Account
        public static IStorageAccount GetAccount(IAzure azure, string accountName, string resourceGroup)
        {
            var account = azure.StorageAccounts.GetByResourceGroup(resourceGroup, accountName);
            return account;
        }

        public static IEnumerable<IStorageAccount> ListAccounts(IAzure azure, string resourceGroup)
        {
            var accounts = azure.StorageAccounts.ListByResourceGroup(resourceGroup);
            return accounts;
        }

        public static IStorageAccount CreateAccount(IAzure azure, string accountName, string resourceGroup, Region region=null)
        {
            if (region == null) region = Core.GetResourceGroup(azure,resourceGroup)?.Region;

            var storageAccount = azure.StorageAccounts.Define(accountName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithGeneralPurposeAccountKind()
                .Create();
            return storageAccount;
        }

        public static void DeleteAccount(IAzure azure, string accountName, string resourceGroup)
        {
            azure.StorageAccounts.DeleteByResourceGroup(resourceGroup, accountName);
        }

        public static void DeleteAccount(IAzure azure, IStorageAccount account)
        {
            azure.StorageAccounts.DeleteById(account.Id);
        }
        #endregion

    }
}
