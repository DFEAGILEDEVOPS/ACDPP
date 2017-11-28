using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Azure.WebJobs;
using VstsApi.Net;
using Newtonsoft.Json;
using System.Collections.Specialized;
using Extensions;
using VstsApi.Net.Classes;
using Dashboard.NetStandard.Core.Classes;
using Dashboard.NetStandard.Classes;
using Microsoft.Azure.WebJobs.Extensions;
using Autofac;
using AzureApi.Net;
using System.Text;
using AzureApi.Client.Net;
using Microsoft.Azure.Management.KeyVault.Fluent.Models;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using System.Web;

namespace Dashboard.Webjobs.Net
{
    public class Functions
    {
        static HashSet<string> ExecutingMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Save the existing project or create a new one
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        public static void SaveProject([QueueTrigger(Queuenames.SaveProject)] string queueMessage, TextWriter log)
        {
            if (Program.Config.IsDevelopment){
                if (ExecutingMethods.Contains(nameof(SaveProject)))return;
                ExecutingMethods.Add(nameof(SaveProject));
            }
            try
            {
                //Get the changed project
                var sourceProject = JsonConvert.DeserializeObject<Project>(queueMessage);

                //Get the old project (if any)
                var allProjects = VstsManager.GetProjects();
                var targetProject = string.IsNullOrWhiteSpace(sourceProject.Id) ? null : allProjects.FirstOrDefault(p => p.Id == sourceProject.Id);
                if (targetProject==null) targetProject=allProjects.FirstOrDefault(p => p.Name.EqualsI(sourceProject.Name));

                //Get the dashboard admin project
                var adminProject = allProjects.FirstOrDefault(p => p.Name.EqualsI(AppSettings.SourceProjectName));

                #region Step 2: Create the project
                //Create the build parameters
                var parameters = new Dictionary<string, string>();
                parameters["oc_project_name"] = sourceProject.Name.ToLower().ReplaceI("_", "-").ReplaceI(" ");
                parameters["oc_build_config_name"] = $"webbapp";
                var appUrl = $"https://{parameters["oc_build_config_name"]}-{parameters["oc_project_name"]}.demo.dfe.secnix.co.uk/";

                string newProjectId = null;
                try
                {
                    if (targetProject == null)
                    {
                        targetProject = VstsManager.CreateProject(sourceProject.Name, sourceProject.Description);
                        newProjectId = targetProject.Id;
                    }
                    else if (!sourceProject.Name.Equals(targetProject.Name) || sourceProject.Description != targetProject.Description)
                    {
                        if (!VstsManager.EditProject(targetProject, sourceProject.Name, sourceProject.Description)) throw new Exception("Could not update project name or description");
                    }
                    if (!targetProject.State.EqualsI("WellFormed")) throw new Exception($"Project is not yet available with state {targetProject.State}.");
                    sourceProject.Id = targetProject.Id;

                    //Add the special properties
                    sourceProject.Properties = VstsManager.GetProjectProperties(sourceProject.Id, ProjectProperties.All);
                    var newProperties = new Dictionary<string, string>();
                    if (!sourceProject.Properties.ContainsKey(ProjectProperties.CreatedBy) || sourceProject.Properties[ProjectProperties.CreatedBy] != AppSettings.ProjectCreatedBy) newProperties[ProjectProperties.CreatedBy] = AppSettings.ProjectCreatedBy;
                    if (!sourceProject.Properties.ContainsKey(ProjectProperties.CostCode) || sourceProject.Properties[ProjectProperties.CostCode]!= sourceProject.CostCode) newProperties[ProjectProperties.CostCode] = sourceProject.CostCode;
                    if (!sourceProject.Properties.ContainsKey(ProjectProperties.CreatedDate)) newProperties[ProjectProperties.CreatedDate] = DateTime.Now.ToString();
                    if (!sourceProject.Properties.ContainsKey(ProjectProperties.AppUrl) || !sourceProject.Properties[ProjectProperties.AppUrl].EqualsI(appUrl)) newProperties[ProjectProperties.AppUrl] = appUrl;
                    if (newProperties.Count > 0)
                    {
                        VstsManager.SetProjectProperties(sourceProject.Id, newProperties);
                        sourceProject.Properties = VstsManager.GetProjectProperties(sourceProject.Id, ProjectProperties.All);
                    }
                    //Mark the project as created properly
                    newProjectId = null;
                }
                finally
                {
                    //Delete the project if we couldnt create properly
                    if (!string.IsNullOrWhiteSpace(newProjectId)) VstsManager.DeleteProject(newProjectId);
                }
                #endregion

                #region Step 12: Update the team members
                var teams = VstsManager.GetTeams(sourceProject.Id);
                var team = teams[0];
                var members = VstsManager.GetMembers(sourceProject.Id, team.Id);

                foreach (var member in sourceProject.Members)
                {
                    //Ensure the user account exists
                    var identity = VstsManager.GetAccountUserIdentity(VstsManager.SourceAccountName, member.EmailAddress);
                    if (identity == null) identity = VstsManager.CreateAccountIdentity(VstsManager.SourceAccountName, member.EmailAddress);
                    var licence = VstsManager.GetIdentityLicence(identity.Id);

                    //Add or change the licence type
                    if (!member.EmailAddress.EqualsI(AppSettings.VSTSAccountEmail) && licence == null || (licence.Value != member.LicenceType && !licence.Value.IsAny(LicenceTypes.Msdn, LicenceTypes.Advanced, LicenceTypes.Professional)))
                        VstsManager.AssignLicenceToIdentity(identity.Id, member.LicenceType);

                    //Ensure the user is a member of the team
                    if (!members.Any(m => m.EmailAddress.EqualsI(member.EmailAddress)))
                        VstsManager.AddUserToTeam(member.EmailAddress, team.Id);
                }

                //Get the new team members
                var newMembers = sourceProject.Members.Where(m => !members.Any(m1 => m1.EmailAddress.EqualsI(m.EmailAddress))).ToList();

                //Remove old users
                foreach (var member in members)
                {
                    if (member.EmailAddress.EqualsI(AppSettings.VSTSAccountEmail)) continue;

                    if (!sourceProject.Members.Any(m => m.EmailAddress.EqualsI(member.EmailAddress)))
                        VstsManager.RemoveUserFromTeam(sourceProject.Id, team.Id, member.EmailAddress);
                }
                #endregion

                #region Step 3: Clone the source repo
                //Check if the repo already exists 
                var repos = VstsManager.GetRepos(sourceProject.Id);
                var repo = repos.Count == 0 ? null : repos[0];

                //Create a new repo
                if (repo == null)
                {
                    var repoId = VstsManager.CreateRepo(sourceProject.Id, sourceProject.Name);
                    if (string.IsNullOrWhiteSpace(repoId)) throw new Exception($"Could not create repo '{sourceProject.Name}'");
                    repo = repos.Count == 0 ? null : repos[0];
                }
                if (repo == null) throw new Exception("Could not create repo '{project.Name}'");

                //copy the sample repo if it doesnt already exist
                var authParameters = new Dictionary<string, string>();
                if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
                {
                    authParameters["username"] = "";
                    authParameters["password"] = AppSettings.VSTSPersonalAccessToken;
                    var serviceEndpoint = VstsManager.CreateEndpoint(sourceProject.Id, $"Temp-Import-{Guid.NewGuid()}", "Git", AppSettings.SourceRepoUrl, "PersonalAccessToken", authParameters);
                    VstsManager.ImportRepo(sourceProject.Id, repo.Id, AppSettings.SourceRepoUrl, serviceEndpoint.Id);
                }
                #endregion

                #region Step 5: Create the Resource Group (if it doesnt already exist)
                var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);
                var groupName = "rg-acdpp-" + sourceProject.Id;
                var groups = Core.ListResourceGroups(azure);
                var group = groups == null ? null : groups.FirstOrDefault(g => g.Name.EqualsI(groupName));
                if (group == null) group = Core.CreateResourceGroup(azure, groupName);
                #endregion

                #region Step 6: Create the Azure App Registration /Service Principle (if it doesnt already exist)

                //Create the app registration (if it doesnt already exist)
                var activeDirectoryHelper = new ActiveDirectoryHelper(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId);
                var directoryClient = activeDirectoryHelper.GetActiveDirectoryClientAsApplication();
                var appRegistrations = activeDirectoryHelper.ListAppRegistrations(directoryClient);
                var appRegistrationName = $"app-ACDPP-{sourceProject.Id}";
                var appRegistration = appRegistrations?.FirstOrDefault(a => a.DisplayName.EqualsI(appRegistrationName));
                if (appRegistration == null) appRegistration = appRegistrations?.FirstOrDefault(a => a.IdentifierUris.Any(u => u.EqualsUrl(appUrl)));
                if (appRegistration != null) appRegistrationName = appRegistration.DisplayName;
                if (appRegistration == null) appRegistration = activeDirectoryHelper.CreateAppRegistration(appRegistrationName, appUrl, directoryClient);

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
                }

                //Add access to the keyvault resources (if it doesnt already exist)
                var resources = new Dictionary<Guid, string>();
                resources[new Guid("f53da476-18e3-4152-8e01-aec403e6edc0")] = "Scope";
                activeDirectoryHelper.AddApplicationResourcePermissions(appRegistration, "cfa8b339-82a2-471a-a3c9-0fc0be7a4093", resources);

                //Get the credentials for authenticating the app
                var vaultClientId = appRegistration.AppId;
                var appKey = Encryption.EncryptData(sourceProject.Id);
                var vaultClientSecret = activeDirectoryHelper.AddApplicationPassword(appRegistration, "DefaultKey", appKey);

                #endregion

                #region Step 7: Create the Azure Key Vault (if it doesnt already exist)
                var vaultName = "kv-acdpp-" + sourceProject.Id.ReplaceI("-").Right(15);
                var vaults = KeyVaultBuilder.ListKeyVaults(azure, group.Name);
                var vault = vaults?.FirstOrDefault(v => v.Name.EqualsI(vaultName));

                //Create the vault with permission to active directory client
                if (vault == null) vault = KeyVaultBuilder.CreateKeyVault(azure, AppSettings.ActiveDirectoryClientId, vaultName, groupName);

                //Add admin app permission to the vault 
                KeyVaultBuilder.AddAccessPolicy(vault, AppSettings.ActiveDirectoryClientId);

                //Add client app permission to the vault 
                KeyVaultBuilder.AddAccessPolicy(vault, vaultClientId);

                #endregion

                #region Step 8: Create the SQL Server and Database
                var sqlModel = new CreateStorageModel()
                {
                    SourceProjectId = sourceProject.Id,
                    VaultClientId = vaultClientId,
                    VaultClientSecret = vaultClientSecret,
                    VaultUri = vault.VaultUri,
                    GroupName = group.Name
                };

                Program.SqlQueue.Enqueue(sqlModel);
                #endregion

                #region Step 9: Create the storage account

                var storageModel = new CreateStorageModel()
                {
                    SourceProjectId = sourceProject.Id,
                    VaultClientId = vaultClientId,
                    VaultClientSecret = vaultClientSecret,
                    VaultUri = vault.VaultUri,
                    GroupName = group.Name
                };

                Program.StorageQueue.Enqueue(storageModel);

                #endregion

                #region Step 9: Create the Redis Cache
                var cacheModel = new CreateCacheModel()
                {
                    SourceProjectId = sourceProject.Id,
                    VaultClientId = vaultClientId,
                    VaultClientSecret = vaultClientSecret,
                    VaultUri = vault.VaultUri,
                    GroupName = group.Name
                };

                Program.CacheQueue.Enqueue(cacheModel);
                #endregion

                #region Step 4: Delete the previous OpenShift environment

                //Get the create build definition
                var definitions = VstsManager.GetDefinitions(adminProject.Id, AppSettings.KillBuildName);
                if (definitions == null) throw new Exception($"Cannot find build definition {AppSettings.KillBuildName} in project {AppSettings.SourceProjectName}");
                var sourceDefinition = definitions.FirstOrDefault(d => d.Name.EqualsI(AppSettings.KillBuildName));
                if (sourceDefinition == null) throw new Exception($"Cannot find build definition {AppSettings.KillBuildName} in project {AppSettings.SourceProjectName}");

                //Create a new build if the last failed
                var build = VstsManager.QueueBuild(adminProject.Id, sourceDefinition.Id.ToInt32(), parameters);

                //Wait for the build to finish
                if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(adminProject.Id, build, 300, false);
                if (build.Status.EqualsI("inProgress")) build = VstsManager.WaitForBuild(sourceProject.Id, build, 300, false);

                //Ensure the build succeeded
                if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
                #endregion

                #region Step 4: Build and deploy the OpenShift environment

                //Get the create build definition
                definitions = VstsManager.GetDefinitions(adminProject.Id, AppSettings.ConfigBuildName);
                if (definitions == null) throw new Exception($"Cannot find build definition {AppSettings.ConfigBuildName} in project {AppSettings.SourceProjectName}");
                sourceDefinition = definitions.FirstOrDefault(d => d.Name.EqualsI(AppSettings.ConfigBuildName));
                if (sourceDefinition == null) throw new Exception($"Cannot find build definition {AppSettings.ConfigBuildName} in project {AppSettings.SourceProjectName}");

                parameters["VaultUrl"] = vault.VaultUri;
                parameters["VaultClientId"] = vaultClientId;
                parameters["VaultClientSecret"] = vaultClientSecret;
                parameters["AzureTenantId"] = AppSettings.AzureTenantId;

                //Create a new build if the last failed
                build = VstsManager.QueueBuild(adminProject.Id, sourceDefinition.Id.ToInt32(), parameters);

                //Wait for the build to finish
                if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(adminProject.Id, build, 300, false);
                if (build.Status.EqualsI("inProgress")) build = VstsManager.WaitForBuild(sourceProject.Id, build, 300, false);

                //Ensure the build succeeded
                if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
                #endregion

                #region Step 10: Copy the source build
                //Get the latest build 
                parameters.Remove("VaultUrl");
                parameters.Remove("VaultClientId");
                parameters.Remove("VaultClientSecret");
                parameters.Remove("AzureTenantId");
                parameters.Add("ProjectTitle", sourceProject.Name);

                var secrets = new Dictionary<string, string>();
                secrets.Add("oc_openshift_credentials", AppSettings.OpenShiftToken);

                //Clone the sample build definition from the source to target project
                sourceDefinition = VstsManager.GetDefinitions(sourceProject.Id, AppSettings.SourceBuildName).FirstOrDefault();
                if (sourceDefinition == null) sourceDefinition = VstsManager.CloneDefinition(AppSettings.SourceProjectName, AppSettings.SourceBuildName, sourceProject.Name, repo, parameters,secrets, true);

                #endregion

                #region Step 11: Build and deploy the sample application
                var builds = VstsManager.GetBuilds(sourceProject.Id, sourceDefinition.Name);
                build = builds?.OrderByDescending(b => b.QueueTime)?.FirstOrDefault();
                //Create a new build if the last failed
                if (build == null || (build.Status.EqualsI("Completed") && build.Result.EqualsI("failed"))) build = VstsManager.QueueBuild(sourceProject.Id, sourceDefinition.Id.ToInt32());

                //Wait for the build to finish
                if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(sourceProject.Id, build,300, false);
                if (build.Status.EqualsI("inProgress")) build = VstsManager.WaitForBuild(sourceProject.Id, build,300, false);

                bool notStarted = build.Status.EqualsI("notStarted");
                
                if (build.Result.EqualsI("Failed")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");

                #endregion

                #region Step 11: Welcome the team members
                //if (newMembers.Count > 0)
                {
                    var project = VstsManager.GetProject(sourceProject.Id);
                    var projectUrl = project.Links["web"].ReplaceI(" ", "%20");
                    var gitUrl = repo.RemoteUrl;

                    var notify = new GovNotifyAPI(AppSettings.GovNotifyClientRef, AppSettings.GovNotifyApiKey, AppSettings.GovNotifyApiTestKey);
                    foreach (var member in sourceProject.Members)
                    //foreach (var member in newMembers)
                        {
                        if (member.EmailAddress.EqualsI(AppSettings.VSTSAccountEmail)) continue;
                        var personalisation = new Dictionary<string, dynamic> { { "name", member.DisplayName }, { "email", member.EmailAddress }, { "project", sourceProject.Name }, { "projecturl", projectUrl }, { "giturl", gitUrl }, { "appurl", appUrl } };
                        notify.SendEmail(member.EmailAddress, AppSettings.WelcomeTemplateId, personalisation);
                    }
                }
                #endregion

                log.WriteLine($"Executed {nameof(SaveProject)}:{sourceProject.Name} successfully");
            }
            finally
            {
                if (Program.Config.IsDevelopment) ExecutingMethods.Remove(nameof(SaveProject));
            }
        }

        public static void DeleteProject([QueueTrigger(Queuenames.DeleteProject)] string queueMessage, TextWriter log)
        {
            var sourceProject = JsonConvert.DeserializeObject<Project>(queueMessage);

            var parameters = new Dictionary<string, string>();
            parameters["oc_project_name"] = sourceProject.Name.ToLower().ReplaceI("_", "-").ReplaceI(" ");
            parameters["oc_build_config_name"] = $"webbapp";
            var appUrl = $"https://{parameters["oc_build_config_name"]}-{parameters["oc_project_name"]}.demo.dfe.secnix.co.uk/";

            var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);

            //Delete the app
            var allProjects = VstsManager.GetProjects();
            var adminProject = allProjects.FirstOrDefault(p => p.Name.EqualsI(AppSettings.SourceProjectName));

            var definitions = VstsManager.GetDefinitions(adminProject.Id, AppSettings.KillBuildName);
            if (definitions == null) throw new Exception($"Cannot find build definition {AppSettings.KillBuildName} in project {AppSettings.SourceProjectName}");
            var sourceDefinition = definitions.FirstOrDefault(d => d.Name.EqualsI(AppSettings.KillBuildName));
            if (sourceDefinition == null) throw new Exception($"Cannot find build definition {AppSettings.KillBuildName} in project {AppSettings.SourceProjectName}");

            //Create a new build if the last failed
            var build = VstsManager.QueueBuild(adminProject.Id, sourceDefinition.Id.ToInt32(), parameters);

            //Wait for the build to finish
            if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(adminProject.Id, build, 300, false);
            if (build.Status.EqualsI("inProgress")) build = VstsManager.WaitForBuild(sourceProject.Id, build, 300, false);

            //Ensure the build succeeded
            if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");

            //Get the resource group
            var groupName = "rg-acdpp-" + sourceProject.Id;
            var groups = Core.ListResourceGroups(azure);
            var group = groups == null ? null : groups.FirstOrDefault(g => g.Name.EqualsI(groupName));

            //Delete the storage
            string accountName = "stracdpp" + sourceProject.Id.ReplaceI("-").Right(16);
            var storageAccount = StorageBuilder.GetAccount(azure, accountName, group.Name);
            if (storageAccount!=null)azure.StorageAccounts.DeleteById(storageAccount.Id);


            //Delete the key vault
            var vaultName = "kv-acdpp-" + sourceProject.Id.ReplaceI("-").Right(15);
            var vaults = KeyVaultBuilder.ListKeyVaults(azure, group.Name);
            var vault = vaults?.FirstOrDefault(v => v.Name.EqualsI(vaultName));
            if (vault != null) azure.Vaults.DeleteById(vault.Id);

            //Delete the SQL server
            string serverName = "sqlsrv-acdpp-" + sourceProject.Id;
            var sqlServer = SqlDatabaseBuilder.GetServer(azure, serverName, group.Name);
            azure.SqlServers.DeleteById(sqlServer.Id);

            //Delete the cache
            string cacheName = "rc-acdpp-" + sourceProject.Id;
            var cache = CacheBuilder.GetCache(azure, cacheName, group.Name);
            if (cache != null) azure.RedisCaches.DeleteById(cache.Id);

            //Delete the app registration 
            var activeDirectoryHelper = new ActiveDirectoryHelper(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId);
            var directoryClient = activeDirectoryHelper.GetActiveDirectoryClientAsApplication();
            var appRegistrations = activeDirectoryHelper.ListAppRegistrations(directoryClient);
            var appRegistrationName = $"app-ACDPP-{sourceProject.Id}";
            var appRegistration = appRegistrations?.FirstOrDefault(a => a.DisplayName.EqualsI(appRegistrationName));
            if (appRegistration == null) appRegistration = appRegistrations?.FirstOrDefault(a => a.IdentifierUris.Any(u => u.EqualsUrl(appUrl)));
            if (appRegistration != null) activeDirectoryHelper.DeleteAppRegistration(appRegistration);

            //Delete the resource group
            if (group != null) azure.ResourceGroups.DeleteByName(group.Name);

            //Delete the VSTS project
            VstsManager.DeleteProject(sourceProject.Id);

            log.WriteLine($"Executed {nameof(DeleteProject)}:{sourceProject.Name} successfully");
        }

        public static void CreateCache([QueueTrigger(Queuenames.CreateCache)] string queueMessage, TextWriter log)
        {
            var model = JsonConvert.DeserializeObject<CreateCacheModel>(queueMessage);

            var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);

            var vaultClient = new VaultClient(model.VaultUri, model.VaultClientId, model.VaultClientSecret, AppSettings.AzureTenantId);
            string cacheName = "rc-acdpp-" + model.SourceProjectId;

            var cache = CacheBuilder.GetCache(azure, cacheName, model.GroupName);
            if (cache == null) cache = CacheBuilder.CreateCache(azure, cacheName, model.GroupName);

            var connectionString = $"{cacheName}.redis.cache.windows.net:6380,password={cache.Key},ssl=True,abortConnect=False";

            var secretId = vaultClient.SetSecret("DefaultCache", connectionString);
        }

        public static void CreateStorage([QueueTrigger(Queuenames.CreateStorage)] string queueMessage, TextWriter log)
        {
            var model = JsonConvert.DeserializeObject<CreateStorageModel>(queueMessage);

            var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);

            var vaultClient = new VaultClient(model.VaultUri, model.VaultClientId, model.VaultClientSecret, AppSettings.AzureTenantId);

            string accountName = "stracdpp" + model.SourceProjectId.ReplaceI("-").Right(16);

            var storageAccount = StorageBuilder.GetAccount(azure, accountName, model.GroupName);
            if (storageAccount == null) storageAccount = StorageBuilder.CreateAccount(azure, accountName, model.GroupName);

            var connectionString = $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={storageAccount.Key};EndpointSuffix=core.windows.net";

            var secretId = vaultClient.SetSecret("DefaultStorage", connectionString);
        }

        public static void CreateSql([QueueTrigger(Queuenames.CreateSql)] string queueMessage, TextWriter log)
        {
            var model = JsonConvert.DeserializeObject<CreateSqlModel>(queueMessage);

            var azure = Core.Authenticate(AppSettings.ActiveDirectoryClientId, AppSettings.ActiveDirectoryClientSecret, AppSettings.AzureTenantId, AppSettings.AzureSubscriptionId);

            var vaultClient = new VaultClient(model.VaultUri, model.VaultClientId, model.VaultClientSecret, AppSettings.AzureTenantId);

            string serverName = "sqlsrv-acdpp-" + model.SourceProjectId;
            string adminUsername = $"{serverName}admin";
            string adminPassword = Encryption.EncryptData(model.SourceProjectId);
            adminPassword = adminPassword.Strip(@" /-=+\");

            var sqlServer = SqlDatabaseBuilder.GetServer(azure, serverName, model.GroupName);
            if (sqlServer == null) sqlServer = SqlDatabaseBuilder.CreateSqlServer(azure, serverName, model.GroupName, adminUsername, adminPassword, AppSettings.AppStartIP, AppSettings.AppEndIP);

            //Always use the server admin login
            if (!adminUsername.EqualsI(sqlServer.AdministratorLogin)) adminUsername = sqlServer.AdministratorLogin;

            //Make sure the admin password is correct 
            sqlServer.Update().WithAdministratorPassword(adminPassword).Apply();

            string databaseName = "db-acdpp-" + model.SourceProjectId;
            var sqlDatabase = SqlDatabaseBuilder.GetDatabase(sqlServer, databaseName);
            if (sqlDatabase == null) sqlDatabase = SqlDatabaseBuilder.CreateDatabase(sqlServer, databaseName);

            string connectionString = $"Server=tcp:{serverName}.database.windows.net,1433;Initial Catalog={databaseName};Persist Security Info=False;User ID={adminUsername};Password={adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var secretId = vaultClient.SetSecret("DefaultConnection", connectionString);
        }

        #region Error handling
        public static void ErrorMonitor([ErrorTrigger("0:10:00", 5, Throttle = "10:00")] TraceFilter filter, TextWriter log)
        {
            var message = filter.GetDetailedMessage(5);

            // log last 5 detailed errors to the Dashboard
            log.WriteLine(message);

            //Send Email to GEO reporting errors
            var notify = Program.ContainerIOC.Resolve<IGovNotifyAPI>();
            var personalisation = new Dictionary<string, dynamic> { { "source", nameof(DeleteProject) }, { "message", message }, { "url", "" }};
            notify.SendEmail(AppSettings.ErrorRecipients, AppSettings.ErrorTemplateId, personalisation);
        }

        #endregion
    }
}
