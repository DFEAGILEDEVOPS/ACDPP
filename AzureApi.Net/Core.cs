using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.Sql;
using Microsoft.Azure.Management.Sql.Fluent;
using System;
using System.Collections.Generic;
using System.Configuration;

namespace AzureApi.Net
{

    public class Core
    {
        public static string VaultUrl = ConfigurationManager.AppSettings["VaultUrl"];
        public static string VaultClientId = ConfigurationManager.AppSettings["VaultClientId"];
        public static string VaultClientSecret = ConfigurationManager.AppSettings["VaultClientSecret"];
        public static string AzureTenantId = ConfigurationManager.AppSettings["AzureTenantId"];
        public static string AzureSubscriptionId = ConfigurationManager.AppSettings["AzureSubscriptionId"];

        internal static IAzure Authenticate()
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
        internal static IResourceGroup GetResourceGroup(string groupName, IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            var group = azure.ResourceGroups.GetByName(groupName);
            return group;
        }

        internal static IEnumerable<IResourceGroup> ListResourceGroups(IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            var groups = azure.ResourceGroups.List();
            return groups;
        }

        internal static IResourceGroup CreateResourceGroup(string groupName, Region region, IAzure azure = null)
        {
            var group = GetResourceGroup(groupName);
            if (group != null) return group;

            if (azure == null) azure = Authenticate();

            group = azure.ResourceGroups.Define(groupName)
                .WithRegion(region)
                .Create();

            return group;
        }

        internal static void DeleteResourceGroup(string groupName, IAzure azure = null)
        {
            if (azure == null) azure = Authenticate();
            azure.ResourceGroups.DeleteByName(groupName);
        }
        #endregion
        
    }
}
