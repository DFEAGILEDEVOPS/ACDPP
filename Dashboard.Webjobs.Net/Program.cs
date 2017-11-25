using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Autofac;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Extensions;
using Dashboard.NetStandard.Core;
using Dashboard.NetStandard.Classes;
using AzureApi.Net;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using AzureApi.Client.Net;
using Microsoft.Azure.ActiveDirectory.GraphClient;

namespace Dashboard.Webjobs.Net
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        public static IContainer ContainerIOC;

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            //Culture is required so UK dates can be parsed correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo(ConfigurationManager.AppSettings["Culture"].ToStringOr("en-GB"));
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            var config = new JobHostConfiguration();
            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
                foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                {
                    Console.WriteLine($@"APPSETTING[""{key}""]={ConfigurationManager.AppSettings[key]}");
                }
            }

            config.UseCore();
            config.UseTimers();
            config.Tracing.ConsoleLevel = TraceLevel.Verbose;

            //Use the cloud storage for WebJob storage
            if (!string.IsNullOrWhiteSpace(AppSettings.AzureStorageConnectionString))
            {
                config.DashboardConnectionString = AppSettings.AzureStorageConnectionString;
                config.StorageConnectionString = AppSettings.AzureStorageConnectionString;
            }

            //Create Inversion of Control container
            ContainerIOC = BuildContainerIoC();

            var sourceProjectId = "8A9FABDE-6783-4BBF-8C8B-858543091475";
            var appUrl = "https://acdpp-test2.com";

            #region Step 7: Create the Resource Group (if it doesnt already exist)
            var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);
            var groupName = "rg-acdpp-" + sourceProjectId;
            var groups = Core.ListResourceGroups(azure);
            var group = groups == null ? null : groups.FirstOrDefault(g => g.Name.EqualsI(groupName));
            if (group == null) group = Core.CreateResourceGroup(azure, groupName);

            #endregion

            #region Step 7: Create the Azure App Registration /Service Principle (if it doesnt already exist)

            //Create the app registration (if it doesnt already exist)
            var activeDirectoryHelper = new ActiveDirectoryHelper(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId);
            var directoryClient = activeDirectoryHelper.GetActiveDirectoryClientAsApplication();
            var appRegistrations = activeDirectoryHelper.ListAppRegistrations(directoryClient);
            var appRegistrationName = $"app-ACDPP-{sourceProjectId}";
            var appRegistration = appRegistrations?.FirstOrDefault(a => a.DisplayName.EqualsI(appRegistrationName));
            if (appRegistration == null) appRegistration = appRegistrations?.FirstOrDefault(a => a.IdentifierUris.Any(u => u.EqualsUrl(appUrl)));
            if (appRegistration != null) appRegistrationName = appRegistration.DisplayName;
            if (appRegistration == null) appRegistration = activeDirectoryHelper.CreateAppRegistration(appRegistrationName, appUrl, directoryClient);

            //Add access to the keyvault resources (if it doesnt already exist)
            var resources = new Dictionary<Guid, string>();
            resources[new Guid("f53da476-18e3-4152-8e01-aec403e6edc0")] = "Scope";
            activeDirectoryHelper.AddApplicationResourcePermissions(appRegistration, "cfa8b339-82a2-471a-a3c9-0fc0be7a4093", resources);

            //Get the credentials for authenticating the app
            var vaultClientId = appRegistration.AppId;
            var appKey = Encryption.EncryptData(sourceProjectId);
            var vaultClientSecret = activeDirectoryHelper.AddApplicationPassword(appRegistration, "DefaultKey", appKey);

            #endregion

            #region Give the new app permission to the entire resource group
            
            #endregion

            #region Step 7: Create the Azure Key Vault (if it doesnt already exist)
            var vaultName = "kv-acdpp-"+sourceProjectId.ReplaceI("-").Right(15);
            var vaults = KeyVaultBuilder.ListKeyVaults(azure, group.Name);
            var vault = vaults?.FirstOrDefault(v => v.Name.EqualsI(vaultName));

            //Create the vault with permission to active directory client
            if (vault == null) vault = KeyVaultBuilder.CreateKeyVault(azure, AppSettings.ActiveDirectoryClientId, vaultName, groupName);

            //Add client app permission to the vault 
            KeyVaultBuilder.AddAccessPolicy(vault,vaultClientId);
            var vaultUrl = vault.VaultUri;


            #endregion

            var vaultClient = new VaultClient(vault.VaultUri,AppSettings.ActiveDirectoryClientId,AppSettings.ActiveDirectoryClientSecret,AppSettings.AzureTenantId);
            var vaultValue = Guid.NewGuid().ToString();
            var vaultKey = $"key-{vaultValue}";
            var vaultKey2=vaultClient.SetSecret(vaultKey, vaultValue);
            var vaultValue2 = vaultClient.GetSecret(vaultKey);

            vaultClient = new VaultClient(vault.VaultUri, vaultClientId, vaultClientSecret, AppSettings.AzureTenantId);
            vaultValue2 = vaultClient.GetSecret(vaultKey);
            vaultValue = Guid.NewGuid().ToString();
            vaultKey = $"key-{vaultValue}";
            vaultKey2 = vaultClient.SetSecret(vaultKey, vaultValue);
            vaultValue2 = vaultClient.GetSecret(vaultKey);

            //TODO
            //var vaultKey = $"{vaultName}-key";
            //var vaultClientId = Convert.ToBase64String(appRegistration.KeyCredentials.FirstOrDefault(k => Encoding.UTF8.GetString(k.CustomKeyIdentifier) == vaultKey).Value);

            //vaultKey = $"{vaultName}-key";

            var host = new JobHost(config);
            host.RunAndBlock();
        }

        public static IContainer BuildContainerIoC()
        {
            var builder = new ContainerBuilder();

            builder.Register(g => new GovNotifyAPI(AppSettings.GovNotifyClientRef, AppSettings.GovNotifyApiKey, AppSettings.GovNotifyApiTestKey)).As<IGovNotifyAPI>();

            return builder.Build();
        }
    }
}
