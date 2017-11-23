using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
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
        public static string VaultUrl = ConfigurationManager.AppSettings["VaultUrl"];
        public static string VaultClientId = ConfigurationManager.AppSettings["VaultClientId"];
        public static string VaultClientSecret = ConfigurationManager.AppSettings["VaultClientSecret"];
        public static string AzureTenantId = ConfigurationManager.AppSettings["AzureTenantId"];
        public static string AzureSubscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];

        public static IAzure Authenticate()
        {
            //=================================================================
            // Authenticate
            var credentials = SdkContext.AzureCredentialsFactory.FromServicePrincipal(VaultClientId,VaultClientSecret, AzureTenantId, AzureEnvironment.AzureGlobalCloud);

            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .Authenticate(credentials)
                .WithSubscription(AzureSubscriptionId);
            return azure;
        }



        #region Resource Group
        public static IResourceGroup GetResourceGroup(string groupName, IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            var group = azure.ResourceGroups.GetByName(groupName);
            return group;
        }

        public static IEnumerable<IResourceGroup> ListResourceGroups(IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            var groups = azure.ResourceGroups.List();
            return groups;
        }

        public static IResourceGroup CreateResourceGroup(string groupName, Region region=null, IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();

            var groups = ListResourceGroups(azure);
            var group = groups==null ? null : groups.FirstOrDefault(g=>g.Name.ToLower()==groupName.ToLower());
            if (group != null) return group;

            if (region == null) region = Region.EuropeWest;

            group = azure.ResourceGroups.Define(groupName)
                .WithRegion(region)
                .Create();

            return group;
        }

        public static void DeleteResourceGroup(string groupName, IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            azure.ResourceGroups.DeleteByName(groupName);
        }

        public static string GetRandomResourceName(string name, int length=24)
        {
            return SdkContext.RandomResourceName(name, length);
        }
        #endregion


    }
}
