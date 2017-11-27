using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Fluent;
using Microsoft.Azure.Management.Redis.Fluent;
using System;
using System.Collections.Generic;

namespace AzureApi.Net
{

    public class CacheBuilder
    {
        #region Redis Cache
        public static IRedisCache GetCache(IAzure azure, string accountName, string resourceGroup)
        {
            var account = azure.RedisCaches.GetByResourceGroup(resourceGroup, accountName);
            return account;
        }

        public static IEnumerable<IRedisCache> ListCaches(IAzure azure, string resourceGroup)
        {
            var accounts = azure.RedisCaches.ListByResourceGroup(resourceGroup);
            return accounts;
        }

        public static IRedisCache CreateCache(IAzure azure, string accountName, string resourceGroup, Region region=null)
        {
            if (region == null) region = Core.GetResourceGroup(azure,resourceGroup)?.Region;

            var cache = azure.RedisCaches.Define(accountName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroup)
                .WithBasicSku()
                .Create();
            return cache;
        }

        public static void DeleteCache(IAzure azure, string accountName, string resourceGroup)
        {
            azure.RedisCaches.DeleteByResourceGroup(resourceGroup, accountName);
        }

        public static void DeleteCache(IAzure azure, IRedisCache account)
        {
            azure.RedisCaches.DeleteById(account.Id);
        }
        #endregion

    }
}
