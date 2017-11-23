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

namespace Dashboard.Webjobs.Net
{
    public class Functions
    {
        /// <summary>
        /// Save the existing project or create a new one
        /// </summary>
        /// <param name="message"></param>
        /// <param name="log"></param>
        public static void SaveProject([QueueTrigger(Queuenames.SaveProject)] string queueMessage, TextWriter log)
        {
            //Get the changed project
            var sourceProject = JsonConvert.DeserializeObject<Project>(queueMessage);

            //Get the old project (if any)
            var allProjects = VstsManager.GetProjects();
            var targetProject = string.IsNullOrWhiteSpace(sourceProject.Id) ? null : allProjects.FirstOrDefault(p => p.Id == sourceProject.Id);

            #region Step 2: Create the project
            //Create the build parameters
            var parameters = new Dictionary<string, string>();
            parameters["oc_project_name"] = sourceProject.Name.ToLower().ReplaceI("_", "-").ReplaceI(" ");
            parameters["oc_build_config_name"] = $"webbapp";
            var jsonParameters = JsonConvert.SerializeObject(parameters);
            var appUrl = $"https://{parameters["oc_build_config_name"]}-{parameters["oc_project_name"]}.demo.dfe.secnix.co.uk/";

            string newProjectId = null;
            try
            {
                if (targetProject == null)
                {
                    targetProject = VstsManager.CreateProject(sourceProject.Name, sourceProject.Description);
                    sourceProject.Id = targetProject.Id;
                    newProjectId = targetProject.Id;
                }
                else if (!sourceProject.Name.Equals(targetProject.Name) || sourceProject.Description != targetProject.Description)
                {
                    if (!VstsManager.EditProject(targetProject, sourceProject.Name, sourceProject.Description)) throw new Exception("Could not update project name or description");
                }
                if (!targetProject.State.EqualsI("WellFormed")) throw new Exception($"Project is not yet available with state {targetProject.State}.");

                //Add the special properties
                sourceProject.Properties = VstsManager.GetProjectProperties(sourceProject.Id, ProjectProperties.All);
                var newProperties = new Dictionary<string,string>();
                if (!sourceProject.Properties.ContainsKey(ProjectProperties.CreatedBy)) newProperties[ProjectProperties.CreatedBy] = sourceProject.Properties[ProjectProperties.CreatedBy];
                if (!sourceProject.Properties.ContainsKey(ProjectProperties.CostCode)) newProperties[ProjectProperties.CostCode] = sourceProject.Properties[ProjectProperties.CostCode];
                if (!sourceProject.Properties.ContainsKey(ProjectProperties.CreatedDate)) newProperties[ProjectProperties.CreatedDate] = DateTime.Now.ToString();
                if (!sourceProject.Properties.ContainsKey(ProjectProperties.AppUrl)) newProperties[ProjectProperties.AppUrl] = appUrl;
                if (newProperties.Count > 0) VstsManager.SetProjectProperties(sourceProject.Id, newProperties);
                //Mark the project as created properly
                newProjectId = null;
            }
            finally
            {
                //Delete the project if we couldnt create properly
                if (!string.IsNullOrWhiteSpace(newProjectId)) VstsManager.DeleteProject(newProjectId);
            }
            #endregion

            #region Step 3: Clone the source repo
            //Get the Repo
            var repos = VstsManager.GetRepos(sourceProject.Id);
            var repo = repos.Count == 0 ? null : repos[0];

            //Create a new repo
            if (repo == null)
            {
                var repoId = VstsManager.CreateRepo(sourceProject.Id, targetProject.Name);
                if (string.IsNullOrWhiteSpace(repoId)) throw new Exception($"Could not create repo '{targetProject.Name}'");
                repo = repos.Count == 0 ? null : repos[0];
            }
            if (repo == null) throw new Exception("Could not create repo '{project.Name}'");

            //copy the sample repo if it doesnt already exist
            var authParameters = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
            {
                authParameters["username"] = "";
                authParameters["password"] = AppSettings.VSTSPersonalAccessToken;
                var serviceEndpoint = VstsManager.CreateEndpoint(targetProject.Id, $"Temp-Import-{Guid.NewGuid()}", "Git", AppSettings.SourceRepoUrl, "PersonalAccessToken", authParameters);
                VstsManager.ImportRepo(sourceProject.Id, repo.Id, AppSettings.SourceRepoUrl, serviceEndpoint.ToString());
            }
            #endregion

            #region Step 4: Build and deploy the OpenShift environment

            //Get the build definition
            var definitions = VstsManager.GetDefinitions(AppSettings.SourceProjectName, AppSettings.ConfigBuildName);
            if (definitions == null) throw new Exception($"Cannot find build definition {AppSettings.ConfigBuildName} in project {AppSettings.SourceProjectName}");
            var sourceDefinition = definitions.FirstOrDefault(d => d.Name.EqualsI(AppSettings.ConfigBuildName));
            if (sourceDefinition == null) throw new Exception($"Cannot find build definition {AppSettings.ConfigBuildName} in project {AppSettings.SourceProjectName}");

            //Get the latest build 
            var builds = VstsManager.GetBuilds(sourceProject.Id, sourceDefinition.Id).Where(b => b.Parameters.EqualsI(jsonParameters));
            var build = builds==null ? null : builds.OrderByDescending(b => b.QueueTime).FirstOrDefault();

            //Create a new build if the last failed
            if (build == null || (build.Status.EqualsI("Completed") && build.Result.EqualsI("failed"))) build = VstsManager.QueueBuild(AppSettings.SourceProjectName, sourceDefinition.Id.ToInt32(), jsonParameters);

            //Wait for the build to finish
            if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(AppSettings.SourceProjectName, build);

            //Ensure the build succeeded
            if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
            #endregion

            #region Step 7: Create the Resource Group
            var azure = Core.Authenticate();
            var groupName = "rg-acdpp-" + targetProject.Id;
            var groups = Core.ListResourceGroups(azure);
            var group = groups == null ? null : groups.FirstOrDefault(g => g.Name.EqualsI(groupName));
            if (group == null) group = Core.CreateResourceGroup(groupName, azure: azure);

            #endregion

            #region Step 7: Create the Azure Key Vault
            var vaultName = Core.GetRandomResourceName("kv-acdpp-",24);
            var vaults = KeyVaultBuilder.ListKeyVaults(group.Name, azure);
            var vault = vaults == null ? null : vaults.FirstOrDefault(v => v.Name.EqualsI(vaultName));
            if (vault == null) vault = KeyVaultBuilder.CreateKeyVault(vaultName, groupName, azure: azure);

            var vaultUrl = vault.VaultUri;

            #endregion

            #region Step 7: Create the Azure App Registration

            var appRegistrationName = "adap-acdpp-" + targetProject.Id;

            var directoryClient = AuthenticationHelper.GetActiveDirectoryClientAsApplication();
            //var appRegistration = ActiveDirectoryHelper.GetAppRegistration(appRegistrationName, directoryClient);
            //if (appRegistration==null) appRegistration = ActiveDirectoryHelper.CreateAppRegistration(appRegistrationName, appUrl, directoryClient);

            //TODO
            //var vaultKey = $"{vaultName}-key";
            //var vaultClientId = Convert.ToBase64String(appRegistration.KeyCredentials.FirstOrDefault(k=>Encoding.UTF8.GetString(k.CustomKeyIdentifier) == vaultKey).Value);
            //var vaultClientSecret = "";
            //vaultKey = $"{vaultName}-key";

            #endregion

            #region Step 7: Create the SQL Server and Database
            string serverName = "sqlsrv-acdpp-"+ targetProject.Id;
            string adminUsername = $"{serverName}admin";
            string adminPassword = Crypto.GeneratePassword();

            var sqlServer = SqlDatabaseBuilder.GetServer(serverName, groupName, azure);
            if (sqlServer == null)sqlServer=SqlDatabaseBuilder.CreateSqlServer(serverName, group.Name, adminUsername, adminPassword, AppSettings.AppStartIP, AppSettings.AppEndIP);
            
            string databaseName = "db-acdpp-" + targetProject.Id;
            var sqlDatabase = SqlDatabaseBuilder.GetDatabase(sqlServer, databaseName);
            if (sqlDatabase == null) sqlDatabase = SqlDatabaseBuilder.CreateDatabase(sqlServer, databaseName);

            string connectionString = $"Server=tcp:{serverName}.database.windows.net,1433;Initial Catalog={databaseName};Persist Security Info=False;User ID={adminUsername};Password={adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            var vaultClient = new VaultClient(vault.VaultUri, AppSettings.VaultClientId, AppSettings.VaultClientSecret, AppSettings.AzureTenantId);
            var secretId = vaultClient.SetSecret("DefaultConnection", connectionString);

            #endregion

            #region Step 5: Copy the source build
            parameters["VaultUrl"] = AppSettings.VaultUrl;//vaultUrl;
            parameters["VaultClientId"] = AppSettings.VaultClientId;//vaultClientId;
            parameters["VaultClientSecret"] = AppSettings.VaultClientSecret;// vaultClientSecret;
            parameters["AzureTenantId"] = AppSettings.AzureTenantId;

            //Clone the sample build definition from the source to target project
            sourceDefinition = VstsManager.GetDefinitions(targetProject.Id, AppSettings.SourceBuildName).FirstOrDefault();
            if (sourceDefinition == null) sourceDefinition = VstsManager.CloneDefinition(AppSettings.SourceProjectName, AppSettings.SourceBuildName, targetProject.Name, repo, parameters, true);

            //Get the latest build 
            builds = VstsManager.GetBuilds(targetProject.Id, sourceDefinition.Id).Where(b => b.Parameters.EqualsI(jsonParameters));
            build = builds==null ? null : builds.OrderByDescending(b => b.QueueTime).FirstOrDefault();
            #endregion

            #region Step 6: Build and deploy the sample application
            //Create a new build if the last failed
            if (build == null || (build.Status.EqualsI("Completed") && build.Result.EqualsI("failed"))) build = VstsManager.QueueBuild(targetProject.Id, sourceDefinition.Id.ToInt32(), jsonParameters);

            //Wait for the build to finish
            if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(sourceProject.Id, build);

            //Ensure the build succeeded
            if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
            #endregion

            #region Step 8: Update the team members
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
                if (!members.Any(m => m.EmailAddress.EqualsI(member.EmailAddress, AppSettings.VSTSAccountEmail)))
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

            #region Step 9: Welcome the team members
            if (newMembers.Count > 0)
            {
                var projectUrl = targetProject.Links["web"];
                var gitUrl = repo.RemoteUrl;

                var notify = new GovNotifyAPI(AppSettings.GovNotifyClientRef,AppSettings.GovNotifyApiKey,AppSettings.GovNotifyApiTestKey);
                foreach (var member in newMembers)
                {
                    var personalisation = new Dictionary<string, dynamic> { { "name", member.DisplayName }, { "email", member.EmailAddress }, { "project", sourceProject.Name }, { "projecturl", projectUrl }, { "giturl", gitUrl }, { "appurl", appUrl } };
                    notify.SendEmail(member.EmailAddress, AppSettings.WelcomeTemplateId, personalisation);
                }
            }
            #endregion

            log.WriteLine($"Executed {nameof(SaveProject)}:{sourceProject.Name} successfully");
        }

        public static void DeleteProject([QueueTrigger(Queuenames.DeleteProject)] string queueMessage, TextWriter log)
        {
            var sourceProject = JsonConvert.DeserializeObject<Project>(queueMessage);

            VstsManager.DeleteProject(sourceProject.Id);

            log.WriteLine($"Executed {nameof(DeleteProject)}:{sourceProject.Name} successfully");
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
            notify.SendEmail(AppSettings.VSTSAccountEmail, AppSettings.ErrorTemplateId, personalisation);
        }

        #endregion
    }
}
