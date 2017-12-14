using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VstsApi.Net;
using Extensions;
using VstsApi.Net.Classes;
using AzureApi.Net;
using AzureApi.Client.Net;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.Management.Fluent;

namespace Builder.Net
{
    public class Builder
    {
        public Builder(string activeDirectoryClientId, string activeDirectoryClientSecret, string azureTenantId, string azureSubscriptionId)
        {
            ActiveDirectoryClientId= activeDirectoryClientId;
            ActiveDirectoryClientSecret= activeDirectoryClientSecret;
            AzureTenantId= azureTenantId;
            AzureSubscriptionId= azureSubscriptionId;
        }

        private readonly string ActiveDirectoryClientId;
        private readonly string ActiveDirectoryClientSecret;
        private readonly string AzureTenantId;
        private readonly string AzureSubscriptionId;

        public SaveProjectModel SaveProject(SaveProjectModel model)
        {
            //Get the old project (if any)
            var allProjects = VstsManager.GetProjects();
            var project = string.IsNullOrWhiteSpace(model.Id) ? null : allProjects.FirstOrDefault(p => p.Id == model.Id);

            string newProjectId = null;
            try
            {
                if (project == null)
                {
                    project = VstsManager.CreateProject(model.Name, model.Description);
                    newProjectId = project.Id;
                }
                else if (!model.Name.Equals(project.Name) || model.Description != project.Description)
                {
                    if (!VstsManager.EditProject(project, model.Name, model.Description)) throw new Exception("Could not update project name or description");
                }
                if (!project.State.EqualsI("WellFormed")) throw new Exception($"Project is not yet available with state {project.State}.");
                project.Properties = VstsManager.GetProjectProperties(project.Id, ProjectProperties.All);

                var newProperties = new Dictionary<string, string>();
                //Add the special properties
                foreach (var key in model.Properties.Keys)
                {
                    if (!project.Properties.ContainsKey(key) || project.Properties[key] != model.Properties[key]) newProperties[key] = model.Properties[key];
                }
                if (newProperties.Count > 0)
                {
                    VstsManager.SetProjectProperties(project.Id, newProperties);
                    model.Properties = VstsManager.GetProjectProperties(project.Id, ProjectProperties.All);
                }
                //Mark the project as created properly
                newProjectId = null;

                return (SaveProjectModel)project;
            }
            finally
            {
                //Delete the project if we couldnt create properly
                if (!string.IsNullOrWhiteSpace(newProjectId)) VstsManager.DeleteProject(newProjectId);
            }
        }

        public void DeleteProject(DeleteProjectModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (string.IsNullOrWhiteSpace(model.ProjectName)) throw new ArgumentNullException(nameof(model.ProjectName));

            //Check the project exists
            var project = VstsManager.GetProject(model.ProjectId);
            if (project == null)
            {
                log.WriteLine($"Project '{project.Name}' does not exist");
                return;
            }

            //Check the project name is correct
            if (project.Name.EqualsI(model.ProjectName))
                throw new ArgumentException(nameof(model.ProjectName), "Incorrect ProjectName");

            //Check no resource groups exists
            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);
            var groups = Core.ListResourceGroups(azure, "VstsProjectId", model.ProjectId);
            if (groups.Any()) throw new Exception("The following associated resource groups still exist on Azure: " + groups.Select(g=>g.Name).ToList().ToDelimitedString(Environment.NewLine));

            //TODO Check no deployments exist


            //TODO Check no remaining app registrations

            //Delete the VSTS project
            VstsManager.DeleteProject(model.ProjectId);
            log.WriteLine($"Project '{model.ProjectName}' successfully deleted");
            return;
        }

        public SaveTeamMembersModel SaveTeamMembers(SaveTeamMembersModel model, TextWriter log)
        {
            if (model.Members.Count < 1) throw new ArgumentException(nameof(model.Members),"Projects must contain at least one member");
            var teams = VstsManager.GetTeams(model.ProjectId);
            var team = teams[0];
            var members = VstsManager.GetMembers(model.ProjectId, team.Id);

            foreach (var member in model.Members)
            {
                //Ensure the user account exists
                var identity = VstsManager.GetAccountUserIdentity(VstsManager.SourceAccountName, member.EmailAddress);
                if (identity == null)
                {
                    identity = VstsManager.CreateAccountIdentity(VstsManager.SourceAccountName, member.EmailAddress);
                    log.WriteLine($"Created user '{member.EmailAddress}' to team '{team.Name}'");
                }

                //Add or change the licence type
                var licence = VstsManager.GetIdentityLicence(identity.Id);
                if (!member.EmailAddress.EqualsI(VstsManager.AdminEmail) && licence == null || (licence.Value != member.LicenceType && !licence.Value.IsAny(LicenceTypes.Msdn, LicenceTypes.Advanced, LicenceTypes.Professional)))
                {
                    VstsManager.AssignLicenceToIdentity(identity.Id, member.LicenceType);
                    log.WriteLine($"Assigned '{member.LicenceType}' licence to user '{member.EmailAddress}'");
                }

                //Ensure the user is a member of the team
                if (!members.Any(m => m.EmailAddress.EqualsI(member.EmailAddress)))
                {
                    VstsManager.AddUserToTeam(member.EmailAddress, team.Id);
                    log.WriteLine($"Removed user '{member.EmailAddress}' from team '{team.Name}'");
                }
            }

            //Remove old users
            foreach (var member in members)
            {
                if (member.EmailAddress.EqualsI(VstsManager.AdminEmail)) continue;

                if (!model.Members.Any(m => m.EmailAddress.EqualsI(member.EmailAddress)))
                {
                    VstsManager.RemoveUserFromTeam(model.ProjectId, team.Id, member.EmailAddress);
                    log.WriteLine($"");
                }
            }

            return model;
        }

        public CreateResourceGroupModel CreateResourceGroup(CreateResourceGroupModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.Region)) throw new ArgumentNullException(nameof(model.Region));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);
            var groups = Core.ListResourceGroups(azure);
            var group = groups?.FirstOrDefault(g => g.Name.EqualsI(model.GroupName));
            if (group == null)
            {
                group = Core.CreateResourceGroup(azure, model.GroupName,model.Region, "VstsProjectId", model.ProjectId);
                log.WriteLine($"Created resource group {group.Name} successfully");
            }
            else if (!group.Tags.ContainsKey("VstsProjectId") || group.Tags["VstsProjectId"] != model.ProjectId)
                throw new Exception($"The resource group '{group.Name}' already exists");
            else if (!group.RegionName.EqualsI(model.Region))
                throw new Exception($"Cannot change region of resource group '{group.Name}' region from {group.RegionName} to {model.Region}");
            else
                log.WriteLine($"Resource group {group.Name} already exists");

            model.GroupId = group.Id;

            return model;
        }

        public void DeleteResourceGroup(DeleteResourceGroupModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (string.IsNullOrWhiteSpace(model.GroupId)) throw new ArgumentNullException(nameof(model.GroupId));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            var groups = Core.ListResourceGroups(azure);
            var group = groups?.FirstOrDefault(g => g.Id==model.GroupId);
            if (group == null)
            {
                log.WriteLine($"Resource group '{group.Name}' does not exist");
            }
            else if (!group.Name.EqualsI(model.GroupName))
                throw new ArgumentException(nameof(model.GroupName), "Group name is incorrect");
            else if (!group.Tags.ContainsKey("VstsProjectId") || group.Tags["VstsProjectId"] != model.ProjectId)
                throw new Exception($"Resource group '{group.Name}' is not associated with project '{model.ProjectId}'");
            else if (!azure.SqlServers.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains Sql Servers");
            else if (!azure.StorageAccounts.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains Storage Accounts");
            else if (!azure.Vaults.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains Key Vaults");
            else if (!azure.VirtualMachines.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains virtual machines");
            else if (!azure.WebApps.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains web apps");
            else if (!azure.VirtualNetworkGateways.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains virtual network gateways");
            else if (!azure.VirtualMachineScaleSets.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains virtual machine scale sets");
            else if (!azure.TrafficManagerProfiles.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains traffic manager profiles");
            else if (!azure.Snapshots.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains snapshots");
            else if (!azure.ServiceBusNamespaces.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains service bus namespaces");
            else if (!azure.SearchServices.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains search services");
            else if (!azure.RedisCaches.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains redis caches");
            else if (!azure.PublicIPAddresses.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains public IP addresses");
            else if (!azure.NetworkWatchers.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains network watchers");
            else if (!azure.NetworkSecurityGroups.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains network security groups");
            else if (!azure.Networks.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains networks");
            else if (!azure.NetworkInterfaces.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains network interfaces");
            else if (!azure.ManagementLocks.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains management locks");
            else if (!azure.LocalNetworkGateways.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains local network gateways");
            else if (!azure.LoadBalancers.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains load balancers");
            else if (!azure.KubernetesClusters.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains kubernetes clusters");
            else if (!azure.ExpressRouteCircuits.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains expressroute circuits");
            else if (!azure.DnsZones.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains DNS zones");
            else if (!azure.Disks.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains disks");
            else if (!azure.Deployments.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains deployments");
            else if (!azure.CosmosDBAccounts.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains CosmosDB accounts");
            else if (!azure.ContainerServices.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains container services");
            else if (!azure.ContainerRegistries.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains container registries");
            else if (!azure.ContainerGroups.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains container groups");
            else if (!azure.CdnProfiles.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains CDN profiles");
            else if (!azure.BatchAccounts.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains batch accounts");
            else if (!azure.AvailabilitySets.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains availability sets");
            else if (!azure.ApplicationGateways.ListByResourceGroup(group.Name).Any())
                throw new Exception($"Resource group still contains application gateways");

            //Delete the resource group
            azure.ResourceGroups.DeleteByName(group.Name);

            log.WriteLine($"Resource group {group.Name} successfully deleted");
        }

        public CreateAppRegistrationModel CreateCreateAppRegistration(CreateAppRegistrationModel model, TextWriter log)
        {
            //Create the app registration (if it doesnt already exist)
            var activeDirectoryHelper = new ActiveDirectoryHelper(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId);
            var directoryClient = activeDirectoryHelper.GetActiveDirectoryClientAsApplication();
            var appRegistrations = activeDirectoryHelper.ListAppRegistrations(directoryClient);

            var appRegistration = appRegistrations?.FirstOrDefault(a => a.IdentifierUris.Any(u => u.EqualsUrl(model.AppUrl)));

            if (appRegistration != null && !appRegistration.DisplayName.EqualsI(model.AppRegistrationName))
                throw new Exception($"Application Registration '{appRegistration.DisplayName}' already exists for Url '{model.AppUrl}'");

            if (appRegistration == null)
            {
                appRegistration = activeDirectoryHelper.CreateAppRegistration(model.AppRegistrationName, model.AppUrl, directoryClient);
                log.WriteLine($"Created App Registration '{model.AppRegistrationName}' for url '{model.AppUrl}'");
            }

            //Create the service principal
            var servicePrinciples = activeDirectoryHelper.GetServicePrinciples(directoryClient);
            var newServicePrincipal = servicePrinciples.FirstOrDefault(sp => sp.AppId == appRegistration.AppId);
            if (newServicePrincipal == null)
            {
                newServicePrincipal = new ServicePrincipal
                {
                    DisplayName = appRegistration.DisplayName,
                    AccountEnabled = true,
                    AppId = appRegistration.AppId
                };
                directoryClient.ServicePrincipals.AddServicePrincipalAsync(newServicePrincipal).Wait();
                log.WriteLine($"Created Service Principle for App Registration '{model.AppRegistrationName}' for url '{model.AppUrl}'");
            }

            //Add access to the keyvault resources (if it doesnt already exist)
            var resources = new Dictionary<Guid, string>();
            resources[new Guid("f53da476-18e3-4152-8e01-aec403e6edc0")] = "Scope";
            activeDirectoryHelper.AddApplicationResourcePermissions(appRegistration, "cfa8b339-82a2-471a-a3c9-0fc0be7a4093", resources);

            //Get the credentials for authenticating the app
            model.AppId = appRegistration.AppId;
            var appKey = Encryption.EncryptData(model.ProjectId);
            model.AppPassword = activeDirectoryHelper.AddApplicationPassword(appRegistration, "DefaultKey", appKey);

            log.WriteLine($"Successfully set password for App Registration '{model.AppRegistrationName}' for url '{model.AppUrl}'");

            return model;
        }

        public void DeleteAppRegistration(DeleteAppRegistrationModel model, TextWriter log)
        {
            //Delete the app registration 
            var activeDirectoryHelper = new ActiveDirectoryHelper(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId);
            var directoryClient = activeDirectoryHelper.GetActiveDirectoryClientAsApplication();
            var appRegistrations = activeDirectoryHelper.ListAppRegistrations(directoryClient);

            var appRegistration = appRegistrations?.FirstOrDefault(a => a.DisplayName.EqualsI(model.AppRegistrationName));
            if (appRegistration != null && !appRegistration.IdentifierUris.Any(u => u.EqualsUrl(model.AppUrl)))
                throw new Exception($"Invalid Url '{model.AppUrl}' for Application Registration '{model.AppRegistrationName}'");

            if (appRegistration == null)
            {
                appRegistration = appRegistrations?.FirstOrDefault(a => a.IdentifierUris.Any(u => u.EqualsUrl(model.AppUrl)));
                if (appRegistration != null && !appRegistration.DisplayName.EqualsI(model.AppRegistrationName))
                    throw new Exception($"Invalid Application Registration name '{model.AppRegistrationName}' for Url '{model.AppUrl}'");
            }

            if (appRegistration == null)
                log.WriteLine($"App Registration '{model.AppRegistrationName}' for url '{model.AppUrl}' does not exist");
            else
            {
                activeDirectoryHelper.DeleteAppRegistration(appRegistration);
                log.WriteLine($"App Registration '{model.AppRegistrationName}' for url '{model.AppUrl}' deleted");
            }
        }

        public SaveKeyVaultModel SaveKeyVault(SaveKeyVaultModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.VaultName)) throw new ArgumentNullException(nameof(model.VaultName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (model.AppIds==null || !model.AppIds.Any()) throw new ArgumentOutOfRangeException(nameof(model.AppIds),"You must specify at least one App Registration Id");

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var vaults = KeyVaultBuilder.ListKeyVaults(azure, group.Name);
            var vault = vaults?.FirstOrDefault(v => v.Name.EqualsI(model.VaultName));

            //Create the vault with permission to active directory client
            if (vault == null)
            {
                vault = KeyVaultBuilder.CreateKeyVault(azure, ActiveDirectoryClientId, model.VaultName, model.GroupName);
                log.WriteLine($"Created vault '{model.VaultName}' in group '{model.GroupName}'");
            }
            else
                log.WriteLine($"Vault '{model.VaultName}' in group '{model.GroupName}' already exists");

            //Add admin app permission to the vault 
            KeyVaultBuilder.AddAccessPolicy(vault, ActiveDirectoryClientId);
            log.WriteLine($"Policy set for Active Directory access to Vault '{model.VaultName}' in group '{model.GroupName}'");

            //Add permission for the application to the vault
            foreach (var appId in model.AppIds)
            {
                KeyVaultBuilder.AddAccessPolicy(vault, appId);
                log.WriteLine($"Policy set for App Registration '{appId}' access to Vault '{model.VaultName}' in group '{model.GroupName}'");
            }
            model.VaultId = vault.Id;
            model.VaultUri = vault.VaultUri;
            return model;
        }

        public void DeleteKeyVault(DeleteKeyVaultModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.VaultName)) throw new ArgumentNullException(nameof(model.VaultName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var vaults = KeyVaultBuilder.ListKeyVaults(azure, group.Name);
            var vault = vaults?.FirstOrDefault(v => v.Name.EqualsI(model.VaultName));

            //Create the vault with permission to active directory client
            if (vault == null)
            {
                log.WriteLine($"Vault '{model.VaultName}' in group '{model.GroupName}' does not exist");
            }
            else
            {
                //Delete the key vault
                azure.Vaults.DeleteById(vault.Id);
                log.WriteLine($"Vault '{model.VaultName}' in group '{model.GroupName}' deleted");
            }
        }

        public CreateCacheModel CreateCache(CreateCacheModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.CacheName)) throw new ArgumentNullException(nameof(model.VaultName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var cache = CacheBuilder.GetCache(azure, model.CacheName, model.GroupName);
            if (cache == null)
            {
                cache = CacheBuilder.CreateCache(azure, model.CacheName, model.GroupName);
                log.WriteLine($"Cache '{model.CacheName}' in group '{model.GroupName}' created");
            }
            else
                log.WriteLine($"Cache '{model.CacheName}' in group '{model.GroupName}' already exists");

            model.ConnectionString = $"{model.CacheName}.redis.cache.windows.net:6380,password={cache.Key},ssl=True,abortConnect=False";

            return model;
        }

        public void DeleteCache(DeleteCacheModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.CacheName)) throw new ArgumentNullException(nameof(model.VaultName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var cache = CacheBuilder.GetCache(azure, model.CacheName, model.GroupName);

            if (cache == null)
            {
                azure.RedisCaches.DeleteById(cache.Id);
                log.WriteLine($"Cache '{model.CacheName}' in group '{model.GroupName}' deleted");
            }
            else
                log.WriteLine($"Cache '{model.CacheName}' in group '{model.GroupName}' does not exist");

        }

        public CreateStorageModel CreateStorage(CreateStorageModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.StorageAccountName)) throw new ArgumentNullException(nameof(model.StorageAccountName));
            if (model.StorageAccountName.Length>24) throw new ArgumentNullException(nameof(model.StorageAccountName),"Storage Account Name must be 254 characters or less");
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var storageAccount = StorageBuilder.GetAccount(azure, model.StorageAccountName, model.GroupName);
            if (storageAccount == null)
            {
                storageAccount = StorageBuilder.CreateAccount(azure, model.StorageAccountName, model.GroupName);
                log.WriteLine($"Storage Account '{model.StorageAccountName}' in group '{model.GroupName}' created");
            }
            else
                log.WriteLine($"Storage Account '{model.StorageAccountName}' in group '{model.GroupName}' already exists");

            model.ConnectionString = $"DefaultEndpointsProtocol=https;AccountName={storageAccount.Name};AccountKey={storageAccount.Key};EndpointSuffix=core.windows.net";

            return model;
        }

        public void DeleteStorage(DeleteStorageModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.StorageAccountName)) throw new ArgumentNullException(nameof(model.StorageAccountName));
            if (model.StorageAccountName.Length > 24) throw new ArgumentNullException(nameof(model.StorageAccountName), "Storage Account Name must be 254 characters or less");
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            var groups = Core.ListResourceGroups(azure);
            var group = groups?.FirstOrDefault(g => g.Name.EqualsI(model.GroupName));
            if (group == null) throw new ArgumentException(nameof(model.GroupName), $"Group '{model.GroupName}' does not exist");

            if (!group.Tags.ContainsKey("VstsProjectId") || group.Tags["VstsProjectId"] != model.ProjectId)
                throw new ArgumentException(nameof(model.GroupName), $"Group '{model.GroupName}' is not associated with this project");

            var storageAccount = StorageBuilder.GetAccount(azure, model.StorageAccountName, model.GroupName);
            if (storageAccount == null)
                log.WriteLine($"Storage Account '{model.StorageAccountName}' in group '{model.GroupName}' does not exist");
            else
            {
                azure.StorageAccounts.DeleteById(storageAccount.Id);
                log.WriteLine($"Storage Account '{model.StorageAccountName}' in group '{model.GroupName}' deleted");
            }
        }

        public SaveSqlServerModel SaveSqlServer(SaveSqlServerModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ServerName)) throw new ArgumentNullException(nameof(model.ServerName));
            if (string.IsNullOrWhiteSpace(model.AdministratorLogin)) throw new ArgumentNullException(nameof(model.AdministratorLogin));
            if (model.FirewallRules.Count==0 && !model.AllowAzureAccess) throw new ArgumentException(nameof(model.FirewallRules),"You must specify at least one firewall rule or allow access to all azure services");

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            //Ensure we always have a password
            if (string.IsNullOrWhiteSpace(model.AdministratorPassword)) model.AdministratorPassword = Crypto.GeneratePasscode(Text.AlphaNumericChars.ToCharArray(), 42);

            var sqlServer = SqlDatabaseBuilder.GetServer(azure, model.ServerName, model.GroupName);
            if (sqlServer == null)
            {
                sqlServer = SqlDatabaseBuilder.CreateSqlServer(azure, model.ServerName, model.GroupName, model.AdministratorLogin, model.AdministratorPassword);
                log.WriteLine($"Sql Server '{sqlServer.Name}' created in resource group '{sqlServer.ResourceGroupName}'");
            }
            else
            {
                SqlDatabaseBuilder.SetPassword(sqlServer, model.AdministratorPassword);
                log.WriteLine($"Password reset for Sql Server '{sqlServer.Name}' in resource group '{sqlServer.ResourceGroupName}' already exists.");
            }

            //Set the firewall rules
            if (model.AllowAzureAccess) model.FirewallRules.Add("0.0.0.0-0.0.0.0");
            foreach (var newRule in model.FirewallRules)
            {
                var startIP = newRule.BeforeFirst("-");
                if (string.IsNullOrEmpty(startIP))startIP = newRule.BeforeFirst(":");
                var endIP = newRule.AfterFirst("-");
                if (string.IsNullOrEmpty(endIP)) endIP = newRule.AfterFirst(":");

                //Create the firewall rule if it doesnt already exist
                var ruleName = string.IsNullOrWhiteSpace(endIP) || startIP == endIP ? $"{startIP.Replace(".", "_")}" : $"{startIP.Replace(".", "_")}-{endIP.Replace(".", "_")}";
                var rule = SqlDatabaseBuilder.GetFirewallRule(sqlServer, ruleName);
                if (rule == null)
                {
                    rule = SqlDatabaseBuilder.CreateFirewallRule(sqlServer, ruleName, startIP, endIP);
                    log.WriteLine($"Firewall '{rule.StartIPAddress}:{rule.EndIPAddress}' created for Sql Server '{sqlServer.Name}' in resource group '{sqlServer.ResourceGroupName}'.");
                }
                else
                {
                    log.WriteLine($"Firewall '{rule.StartIPAddress}:{rule.EndIPAddress}' already exists for Sql Server '{sqlServer.Name}' in resource group '{sqlServer.ResourceGroupName}'.");
                }
            }
            return model;
        }

        public void DeleteSqlServer(DeleteSqlServerModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ServerName)) throw new ArgumentNullException(nameof(model.ServerName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var server = SqlDatabaseBuilder.GetServer(azure,model.ServerName, model.GroupName);
            if (server == null)
            {
                log.WriteLine($"Sql Server '{model.ServerName}' does not exists in resource group '{model.GroupName}'");
                return;
            }

            var count = server.Databases.List().Count;
            if (count>0) throw new Exception($"Sql Server '{server.Name}' still contains {count} database(s)");

            
            //Delete the SQL Server
            azure.SqlServers.DeleteById(server.Id);
            log.WriteLine($"Sql Server '{model.ServerName}' in group '{model.GroupName}' deleted");
        }

        public CreateSqlDatabaseModel CreateSqlDatabase(CreateSqlDatabaseModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ServerName)) throw new ArgumentNullException(nameof(model.ServerName));
            if (string.IsNullOrWhiteSpace(model.DatabaseName)) throw new ArgumentNullException(nameof(model.DatabaseName));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var sqlServer = SqlDatabaseBuilder.GetServer(azure, model.ServerName, model.GroupName);
            if (sqlServer == null) throw new ArgumentException(nameof(model.ServerName),$"Sql Server '{model.ServerName}' does not exist in resource group '{model.GroupName}'");

            var sqlDatabase = SqlDatabaseBuilder.GetDatabase(sqlServer, model.DatabaseName);
            if (sqlDatabase == null)
            {
                sqlDatabase = SqlDatabaseBuilder.CreateDatabase(sqlServer, model.DatabaseName, model.PricingTier);
                log.WriteLine($"Sql database '{sqlDatabase.Name}'({model.PricingTier}) on server '{sqlServer.Name}' successfully created.");
            }
            else
            {
                //Change the pricing tier
                if (!sqlDatabase.ServiceLevelObjective.EqualsI(model.PricingTier))
                {
                    var oldTier = sqlDatabase.ServiceLevelObjective;
                    sqlDatabase.Update().WithServiceObjective(model.PricingTier).Apply();
                    log.WriteLine($"Sql database '{sqlDatabase.Name}'({oldTier}) on server '{sqlServer.Name}' updated to {sqlDatabase.ServiceLevelObjective}");
                }
                else
                    log.WriteLine($"Sql database '{sqlDatabase.Name}'({model.PricingTier}) on server '{sqlServer.Name}' already exists.");

            }

            return model;
        }

        public void DeleteSqlDatabase(DeleteSqlDatabaseModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.ServerName)) throw new ArgumentNullException(nameof(model.ServerName));
            if (string.IsNullOrWhiteSpace(model.GroupName)) throw new ArgumentNullException(nameof(model.GroupName));
            if (string.IsNullOrWhiteSpace(model.ProjectId)) throw new ArgumentNullException(nameof(model.ProjectId));

            var azure = Core.Authenticate(ActiveDirectoryClientId, ActiveDirectoryClientSecret, AzureTenantId, AzureSubscriptionId);

            //Check the group belongs to this project
            CheckProjectGroup(azure, model.GroupName, model.ProjectId);

            var server = SqlDatabaseBuilder.GetServer(azure, model.ServerName, model.GroupName);
            if (server == null)throw new Exception($"Sql Server '{model.ServerName}' does not exists in resource group '{model.GroupName}'");

            var database=SqlDatabaseBuilder.GetDatabase(server, model.DatabaseName);
            if (database == null)
            {
                log.WriteLine($"Sql Database '{model.DatabaseName}' does not exists on Sql Server '{model.ServerName}'");
                return;
            }

            //Delete the database
            database.Delete();
            log.WriteLine($"Sql Database '{model.DatabaseName}' on Sql Server '{model.ServerName}' deleted");
        }

        public CreateRepoModel CreateRepo(CreateRepoModel model, TextWriter log)
        {
            //Check if the repo already exists 
            var repos = VstsManager.GetRepos(model.ProjectId);
            var repo = repos?.FirstOrDefault(r=>r.Name.EqualsI(model.RepoName));

            //Create a new repo
            if (repo == null)
            {
                var repoId = VstsManager.CreateRepo(model.ProjectId, model.RepoName);
                if (string.IsNullOrWhiteSpace(repoId)) throw new Exception($"Could not create repo '{model.RepoName}'");
                repo = repos?.FirstOrDefault(r => r.Name.EqualsI(model.RepoName));
                log.WriteLine($"Repo '{repo.Name}' created successfully");
            }
            else
            {
                log.WriteLine($"Repo '{repo.Name}' already exists");
            }
            model.Url = repo.Url;
            return model;
        }

        public CopyRepoModel CopyRepo(CopyRepoModel model, TextWriter log)
        {
            if (string.IsNullOrWhiteSpace(model.SourceProjectId)) throw new ArgumentNullException(nameof(model.SourceProjectId));
            if (string.IsNullOrWhiteSpace(model.SourceRepoName)) throw new ArgumentNullException(nameof(model.SourceRepoName));
            if (string.IsNullOrWhiteSpace(model.TargetProjectId)) throw new ArgumentNullException(nameof(model.TargetProjectId));
            if (string.IsNullOrWhiteSpace(model.TargetRepoName)) throw new ArgumentNullException(nameof(model.TargetRepoName));

            var targetProject = VstsManager.GetProject(model.TargetProjectId);
            if (targetProject == null) throw new ArgumentException(nameof(model.TargetProjectId), $"Target Project '{model.TargetProjectId}' does not exist");

            var targetRepos = VstsManager.GetRepos(model.SourceProjectId);
            var targetRepo = targetRepos?.FirstOrDefault(r => r.Name.EqualsI(model.TargetRepoName));
            if (targetRepo != null)
            {
                log.WriteLine($"Repo '{model.TargetRepoName}' already exists in project '{targetProject.Name}'");
                model.TargetUrl = targetRepo.Url;
                return model;
            }

            var sourceProject = VstsManager.GetProject(model.SourceProjectId);
            if (sourceProject == null) throw new ArgumentException(nameof(model.SourceProjectId), $"Source Project '{model.SourceProjectId}' does not exist");

            //Check if the repo already exists 
            var sourceRepos = VstsManager.GetRepos(model.SourceProjectId);
            var sourceRepo = sourceRepos?.FirstOrDefault(r => r.Name.EqualsI(model.SourceRepoName));
            if (sourceRepo == null) throw new Exception($"Repo '{model.SourceRepoName}' does not exist in project '{sourceProject.Name}'");

            //Create a new repo
            var repoId = VstsManager.CreateRepo(targetProject.Id, model.TargetRepoName);
            if (string.IsNullOrWhiteSpace(repoId)) throw new Exception($"Could not create target repo '{model.TargetRepoName}' in project '{targetProject.Name}'");

            targetRepos = VstsManager.GetRepos(model.SourceProjectId);
            targetRepo = targetRepos?.FirstOrDefault(r => r.Name.EqualsI(model.TargetRepoName));

            log.WriteLine($"Repo '{sourceRepo.Name}' created successfully");
            model.TargetUrl = targetRepo.Url;

            //copy the sample repo if it doesnt already exist
            var authParameters = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
            {
                authParameters["username"] = "";
                authParameters["password"] = AppSettings.VSTSPersonalAccessToken;
                var serviceEndpoint = VstsManager.CreateEndpoint(sourceProject.Id, $"Temp-Import-{Guid.NewGuid()}", "Git", sourceRepo.Url, "PersonalAccessToken", authParameters);
                VstsManager.ImportRepo(sourceProject.Id, targetRepo.Id, sourceRepo.Url, serviceEndpoint.Id);
            }
            return model;
        }

        public void CopyBuild(CopyBuildModel model, TextWriter log)
        {

        }

        public void CopyRelease(CopyReleaseModel model, TextWriter log)
        {

        }

        public void QueueBuild(QueueBuildModel model, TextWriter log)
        {

        }

        public void SendEmail(SendEmailModel model, TextWriter log)
        {
            
        }


        private void CheckProjectGroup(IAzure azure, string groupName, string projectId)
        {
            var group = Core.GetResourceGroup(azure, groupName);
            if (group == null) throw new ArgumentException(nameof(groupName), $"Group '{groupName}' does not exist");

            if (!group.Tags.ContainsKey("VstsProjectId") || group.Tags["VstsProjectId"] != projectId)
                throw new ArgumentException(nameof(groupName), $"Group '{groupName}' is not associated with project '{projectId}'");
        }

    }
}
