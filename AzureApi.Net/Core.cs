using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Fluent;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace AzureApi.Net
{

    public class Core
    {
        public static AzureCredentials GetCredentials(string clientId, string clientSecret, string tenantId)
        {
            return SdkContext.AzureCredentialsFactory.FromServicePrincipal(clientId, clientSecret, tenantId, AzureEnvironment.AzureGlobalCloud);
        }

        public static IAzure Authenticate(string clientId, string clientSecret, string tenantId,string subscriptionId)
        {
            //=================================================================
            // Authenticate
            var credentials = GetCredentials(clientId, clientSecret, tenantId);

            return Authenticate(credentials, subscriptionId);
        }

        public static IAzure Authenticate(AzureCredentials credentials, string subscriptionId)
        {
            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(subscriptionId);
            return azure;
        }


        #region Resource Group
        public static IResourceGroup GetResourceGroup(IAzure azure, string groupName)
        {
            var group = azure.ResourceGroups.GetByName(groupName);
            return group;
        }

        public static IEnumerable<IResourceGroup> ListResourceGroups(IAzure azure)
        {
            var groups = azure.ResourceGroups.List();
            return groups;
        }

        public static IResourceGroup CreateResourceGroup(IAzure azure, string groupName, Region region=null)
        {
            var groups = ListResourceGroups(azure);
            var group = groups==null ? null : groups.FirstOrDefault(g=>g.Name.ToLower()==groupName.ToLower());
            if (group != null) return group;

            if (region == null) region = Region.EuropeWest;

            group = azure.ResourceGroups.Define(groupName)
                .WithRegion(region)
                .Create();

            return group;
        }

        public static void DeleteResourceGroup(IAzure azure, string groupName)
        {
            azure.ResourceGroups.DeleteByName(groupName);
        }

        public static string GetRandomResourceName(string name, int length=24)
        {
            return SdkContext.RandomResourceName(name, length);
        }
        #endregion


    }
}
