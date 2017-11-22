using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation;
using Microsoft.TeamFoundation.Framework.Common;
using Microsoft.VisualStudio.Services.Identity;
using Microsoft.VisualStudio.Services.Identity.Client;
using Microsoft.VisualStudio.Services.Licensing;
using Microsoft.VisualStudio.Services.Licensing.Client;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Extensions;
using System.Configuration;
using Task = System.Threading.Tasks.Task;
using Build = VstsApi.Net.Classes.Build;
using Process = VstsApi.Net.Classes.Process;
using System.Collections.Specialized;
using Extensions.Net;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using Microsoft.TeamFoundation.Build.WebApi;
using VstsApi.Net.Classes;

namespace VstsApi.Net
{
    public class VstsManager
    {
        public static string SourceAccountName = ConfigurationManager.AppSettings["SourceAccountName"];
        public static string SourceProjectName = ConfigurationManager.AppSettings["SourceProjectName"];
        public static string SourceRepoName = ConfigurationManager.AppSettings["SourceRepoName"];
        public static string SourceBuildName = ConfigurationManager.AppSettings["SourceBuildName"];

        public static string VSTSAccountEmail = ConfigurationManager.AppSettings["VSTSAccountEmail"];
        public static string VSTSPersonalAccessToken = ConfigurationManager.AppSettings["VSTSPersonalAccessToken"];

        public static string SourceInstanceUrl = $"https://{SourceAccountName}.visualstudio.com/";
        public static string SourceProjectUrl = $"{SourceInstanceUrl}{SourceProjectName}";
        public static string SourceCollectionUrl = $"{SourceInstanceUrl}DefaultCollection";
        public static string SourceRepoUrl = $"{SourceProjectUrl}/_git/{SourceRepoName}";

        public static string TargetProjectTemplateId = ConfigurationManager.AppSettings["TargetProjectTemplateId"];

        const string ApiVersion = "1.0";
        private static readonly int intervalInSec=2;
        private static readonly int maxOpTimeInSeconds=60;
        #region Accounts
        private static VssConnection GetConnection(string collectionUrl, string personalAccessToken)
        {
            return new VssConnection(new Uri(collectionUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        }

        public static Classes.Identity GetAccountUserIdentity(string accountName, string userEmail)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var identityClient = connection.GetClient<IdentityHttpClient>();
                var identity=identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();
                return identity==null ? null : new Classes.Identity
                {
                    Id = identity.Id,
                    UniqueUserId = identity.UniqueUserId,
                    CustomDisplayName = identity.CustomDisplayName,
                    DisplayName = identity.DisplayName,
                    IsContainer = identity.IsContainer,
                    LocalScopeId = identity.LocalScopeId,
                    ProviderDisplayName = identity.ProviderDisplayName,
                    ResourceVersion = identity.ResourceVersion,
                    SubjectDescriptor = identity.SubjectDescriptor,
                    IsActive = identity.IsActive,
                };
            }
        }
        public static List<Classes.Identity>GetAccountUserIdentities(string accountName)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var identityClient = connection.GetClient<IdentityHttpClient>();
                return identityClient.ReadIdentitiesAsync(IdentitySearchFilter.General,"").Result.Select(identity=> new Classes.Identity()
                {
                    Id = identity.Id,
                    UniqueUserId = identity.UniqueUserId,
                    CustomDisplayName = identity.CustomDisplayName,
                    DisplayName = identity.DisplayName,
                    IsContainer = identity.IsContainer,
                    LocalScopeId = identity.LocalScopeId,
                    ProviderDisplayName = identity.ProviderDisplayName,
                    ResourceVersion = identity.ResourceVersion,
                    SubjectDescriptor = identity.SubjectDescriptor,
                    IsActive = identity.IsActive,
                }).ToList();
            }
        }

        public static Classes.Identity CreateAccountIdentity(string accountName, string userEmail)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {

                // We need the clients for two services: Licensing and Identity
                var licensingClient = connection.GetClient<LicensingHttpClient>();
                var identityClient = connection.GetClient<IdentityHttpClient>();

                // The first call is to see if the user already exists in the account.
                // Since this is the first call to the service, this will trigger the sign-in window to pop up.
                Console.WriteLine("Sign in as the admin of account {0}. You will see a sign-in window on the desktop.", accountName);
                var identity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();

                // If the identity is null, this is a user that has not yet been added to the account.
                // We'll need to add the user as a "bind pending" - meaning that the email address of the identity is 
                // recorded so that the user can log into the account, but the rest of the details of the identity 
                // won't be filled in until first login.
                if (identity == null)
                {
                    Console.WriteLine("Creating a new identity and adding it to the collection's licensed users group.");

                    // We are adding the user to a collection, and at the moment only one collection is supported per
                    // account in VSTS.
                    var collectionScope = identityClient.GetScopeAsync(accountName).Result;

                    // First get the descriptor for the licensed users group, which is a well known (built in) group.
                    var licensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType, GroupWellKnownSidConstants.LicensedUsersGroupSid);

                    // Now convert that into the licensed users group descriptor into a collection scope identifier.
                    var identifier = String.Concat(SidIdentityHelper.GetDomainSid(collectionScope.Id), SidIdentityHelper.WellKnownSidType, licensedUsersGroupDescriptor.Identifier.Substring(SidIdentityHelper.WellKnownSidPrefix.Length));

                    // Here we take the string representation and create the strongly-type descriptor
                    var collectionLicensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType, identifier);


                    // Get the domain from the user that runs this code. This domain will then be used to construct
                    // the bind-pending identity. The domain is either going to be "Windows Live ID" or the Azure 
                    // Active Directory (AAD) unique identifier, depending on whether the account is connected to
                    // an AAD tenant. Then we'll format this as a UPN string.
                    var currUserIdentity = connection.AuthorizedIdentity.Descriptor;
                    var directory = "Windows Live ID"; // default to an MSA (fka Live ID)
                    if (currUserIdentity.Identifier.Contains('\\'))
                    {
                        // The identifier is domain\userEmailAddress, which is used by AAD-backed accounts.
                        // We'll extract the domain from the admin user.
                        directory = currUserIdentity.Identifier.Split(new char[] { '\\' })[0];
                    }
                    var upnIdentity = string.Format("upn:{0}\\{1}", directory, userEmail);

                    // Next we'll create the identity descriptor for a new "bind pending" user identity.
                    var newUserDesciptor = new IdentityDescriptor(IdentityConstants.BindPendingIdentityType, upnIdentity);

                    // We are ready to actually create the "bind pending" identity entry. First we have to add the
                    // identity to the collection's licensed users group. Then we'll retrieve the Identity object
                    // for this newly-added user. Without being added to the licensed users group, the identity 
                    // can't exist in the account.
                    bool result = identityClient.AddMemberToGroupAsync(collectionLicensedUsersGroupDescriptor, newUserDesciptor).Result;
                    identity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();
                }
                return identity == null ? null : new Classes.Identity
                {
                    Id = identity.Id,
                    UniqueUserId = identity.UniqueUserId,
                    CustomDisplayName = identity.CustomDisplayName,
                    DisplayName = identity.DisplayName,
                    IsContainer = identity.IsContainer,
                    LocalScopeId = identity.LocalScopeId,
                    ProviderDisplayName = identity.ProviderDisplayName,
                    ResourceVersion = identity.ResourceVersion,
                    SubjectDescriptor = identity.SubjectDescriptor,
                    IsActive = identity.IsActive,
                };
            }
        }

        public static List<Build> GetBuilds(string projectName, string buildName, params string[] parameters)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {

                var projectClient = connection.GetClientAsync<ProjectHttpClient>().Result;
                var projects = projectClient.GetProjects(ProjectState.WellFormed).Result;
                var project = projects?.FirstOrDefault(p => p.Name.EqualsI(projectName));
                if (project == null) throw new ArgumentException($"Project '{projectName}' does not exist");

                var buildClient = connection.GetClientAsync<BuildHttpClient>().Result;
                var sourceBuildRefs = buildClient.GetDefinitionsAsync(project.Id).Result;

                var buildRef = sourceBuildRefs?.FirstOrDefault(b => b.Name.EqualsI(buildName));
                if (buildRef == null) throw new ArgumentException($"Build '{buildName}' does not exist");

                var builds = buildClient.GetBuildsAsync(project.Id,new[] { buildRef.Id }).Result;
                return builds.Select(b => new Classes.Build { Id = b.Id.ToString(), Url = b.Url, Status=b.Status.Value.ToString(),StartTime=b.StartTime.Value.ToString(), FinishTime=b.FinishTime.Value.ToString(), SourceBranch=b.SourceBranch, Result=b.Result.Value.ToString(), Reason=b.Reason.ToString(), QueueTime=b.QueueTime.Value.ToString() }).ToList();
            }
        }

        public static Definition CloneDefinition(string sourceProjectName, string sourceBuildName, string targetProjectName, Repo repo, Dictionary<string,string> variables=null, bool overwrite=false)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var projectClient = connection.GetClientAsync<ProjectHttpClient>().Result;
                var projects=projectClient.GetProjects(ProjectState.WellFormed).Result;
                var sourceProject = projects?.FirstOrDefault(p => p.Name.EqualsI(sourceProjectName));
                if (sourceProject == null) throw new ArgumentException($"Source Project '{sourceProjectName}' does not exist");

                var buildClient = connection.GetClientAsync<BuildHttpClient>().Result;
                var sourceDefinitions = buildClient.GetDefinitionsAsync(sourceProject.Id).Result;
                var sourceBuildRef = sourceDefinitions?.FirstOrDefault(b=>b.Name.EqualsI(SourceBuildName));
                if (sourceBuildRef == null) throw new ArgumentException($"Source Build '{sourceBuildName}' does not exist");

                var sourceBuild = buildClient.GetDefinitionAsync(sourceProject.Id,sourceBuildRef.Id).Result;


                var targetProject = projects?.FirstOrDefault(p => p.Name.EqualsI(targetProjectName)) as TeamProjectReference;
                if (sourceProject == null) throw new ArgumentException($"Target Project '{sourceProjectName}' does not exist");

                var targetBuildRef = buildClient.GetDefinitionsAsync(targetProject.Id, sourceBuildName).Result.FirstOrDefault();

                if (targetBuildRef != null)
                {
                    if (overwrite)
                        buildClient.DeleteDefinitionAsync(targetProject.Id, targetBuildRef.Id);
                    else
                        throw new ArgumentException($"Target Build '{sourceBuildName}' already exists");
                }

                sourceBuild.Project = null;

                //Check the repo service endpoint is available
                var connectedServiceId = sourceBuild.Repository.Properties["connectedServiceId"];
                Endpoint sourceEndpoint = null;
                Endpoint targetEndpoint = null;
                if (!string.IsNullOrWhiteSpace(connectedServiceId))
                {
                    targetEndpoint = GetEndpoint(targetProject.Id.ToString(), connectedServiceId);

                    if (targetEndpoint == null)
                    {
                        sourceEndpoint = GetEndpoint(sourceProject.Id.ToString(), connectedServiceId);

                        targetEndpoint = GetEndpoints(targetProject.Id.ToString()).FirstOrDefault(e => e.Type.EqualsI(sourceEndpoint.Type) && e.Url.EqualsI(sourceEndpoint.Url) && e.Authorization.Scheme.EqualsI(sourceEndpoint.Authorization.Scheme));
                        //Create a new endpoint
                        var authParameters = new Dictionary<string, string>();
                        authParameters["username"] = "";
                        authParameters["password"] = VSTSPersonalAccessToken;

                        if (targetEndpoint == null)targetEndpoint = CreateEndpoint(targetProject.Id.ToString(), sourceEndpoint.Name, sourceEndpoint.Type, sourceEndpoint.Url, sourceEndpoint.Authorization.Scheme, authParameters);

                    }
                    sourceBuild.Repository.Properties["connectedServiceId"] = targetEndpoint.Id;
                }

                if (repo.Project.Id.ToGuid()==targetProject.Id && repo.Name.EqualsI(sourceBuild.Repository.Name))
                {
                    sourceBuild.Repository.Id = repo.Id;
                    sourceBuild.Repository.Name = repo.Name;
                    sourceBuild.Repository.DefaultBranch = repo.DefaultBranch;
                    sourceBuild.Repository.Url = new Uri(repo.RemoteUrl);
                }

                var queue = GetQueues(targetProject.Id.ToString(),sourceBuild.Queue.Name).FirstOrDefault();
                if (queue == null)
                    sourceBuild.Queue = null;
                else if (sourceBuild.Queue.Id != queue.Id.ToInt32())
                    sourceBuild.Queue.Id = queue.Id.ToInt32();//TODO - keeps throwing "No agent queue found with identifier 302".

                if (variables != null)
                    foreach (var key in variables.Keys)
                        sourceBuild.Variables[key].Value = variables[key];

                sourceBuild.VariableGroups.Clear();
                var targetBuild =buildClient.CreateDefinitionAsync(sourceBuild, targetProject.Id);
                targetBuild.Wait();
                var definition = targetBuild?.Result;
                return GetDefinitions(targetProject.Id.ToString(),definition.Name).FirstOrDefault();
            }
        }

        public static List<Definition> GetDefinitions(string project, string name=null)
        {
            var apiVersion = "2.0";

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/build/definitions?api-version={apiVersion}{(string.IsNullOrWhiteSpace(name) ? "" : "&name="+name)}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Definition>();
            foreach (var item in json.value)
            {
                results.Add(new Definition()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    Url = (string)item.url,
                    Revision = (string)item.revision
                });
            }
            return results;
        }

        public static List<Queue> GetQueues(string project, string queueName=null)
        {
            var apiVersion = "3.0-preview.1";

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/distributedtask/queues?api-version={apiVersion}{(string.IsNullOrWhiteSpace(queueName)?"":$"&queueName={queueName}")}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Queue>();
            foreach (var item in json.value)
            {
                results.Add(new Queue()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    GroupScopeId = (string)item.groupScopeId,
                    Pool=new QueuePool()
                    {
                        Id = (string)item.pool.id,
                        Name = (string)item.pool.name,
                        Scope = (string)item.pool.scope,
                    }
                });
            }
            return results;
        }

        public static List<Process> GetProcesses()
        {

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/_apis/process/processes?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Process>();
            foreach (var item in json.value)
            {
                results.Add(new Process()
                {
                    Id = (string)item.id,
                    Url = (string)item.url,
                    Name = (string)item.name,
                    Description = (string)item.description,
                    IsDefault = (string)item.isDefault
                });
            }
            return results;
        }

        public static List<Build> GetBuilds(string project, string build)
        {
            var definitionId=build.IsInteger() ? build : GetDefinitions(project,build).FirstOrDefault()?.Id;
            var apiVersion = "2.0";

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/build/builds?api-version={apiVersion}&definitions={definitionId}&statusFilter=all", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Build>();
            foreach (var item in json.value)
            {
                results.Add(new Build()
                {
                    Id = (string)item.id,
                    Url = (string)item.url,
                    Definition = new Definition()
                    {
                        Id= (string)item.definition.id,
                        Name= (string)item.definition.name,
                        Url = (string)item.definition.url,
                        Revision= (string)item.definition.revision,
                    },
                    SourceBranch = (string)item.sourceBranch,
                    Status= (string)item.status,
                    QueueTime= (string)item.queueTime,
                    StartTime = (string)item.startTime,
                    FinishTime= (string)item.finishTime,
                    Parameters = (string)item.parameters,
                    Result = (string)item.result,
                    Reason = (string)item.reason,
                });
            }
            return results;
        }

        public static Build GetBuild(string project, int buildId)
        {
            var apiVersion = "2.0";

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/build/builds/{buildId}?api-version={apiVersion}", password: VSTSPersonalAccessToken)).Result;
            var result = new Build()
            {
                Id = (string)json.id,
                Url = (string)json.url,
                Definition = new Definition()
                {
                    Id = (string)json.definition.id,
                    Name = (string)json.definition.name,
                    Url = (string)json.definition.url,
                    Revision = (string)json.definition.revision,
                },
                SourceBranch = (string)json.sourceBranch,
                Status = (string)json.status,
                QueueTime = (string)json.queueTime,
                StartTime = (string)json.startTime,
                Parameters = (string)json.parameters,
                Result = (string)json.result,
                Reason = (string)json.reason,
            };
            
            return result;
        }

        public static Build QueueBuild(string project, int definitionId, string parameters = null, string branch = null)
        {
            var apiVersion = "2.0";

            if (string.IsNullOrWhiteSpace(branch))
                branch = "refs/heads/master";
            else if (!branch.ContainsI("/"))
                branch = $"refs/heads/{branch}";

             var body = new
            {
                definition = new { id = definitionId },
                sourceBranch = branch,
                parameters = parameters
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{SourceCollectionUrl}/{project}/_apis/build/builds?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;

            var result = new Build()
            {
                Id = (string)json.id,
                Url = (string)json.url,
                Definition = new Definition()
                {
                    Id = (string)json.definition.id,
                    Name = (string)json.definition.name,
                    Url = (string)json.definition.url,
                    Revision = (string)json.definition.revision,
                },
                SourceBranch = (string)json.sourceBranch,
                Status = (string)json.status,
                Result = (string)json.result,
                QueueTime = (string)json.queueTime,
                StartTime = (string)json.startTime,
                FinishTime = (string)json.finishTime,
                Parameters = (string)json.parameters,
                Reason = (string)json.reason,
            };

            return result;
        }

        public static Build WaitForBuild(string project,Build build, int timeoutSeconds=120)
        {
            DateTime expiration = DateTime.Now.AddSeconds(timeoutSeconds);
            while (!build.Status.EqualsI("Completed"))
            {
                if (DateTime.Now > expiration) throw new Exception($"Build '{build.Definition.Name}:{build.Id}' did not complete in {timeoutSeconds} seconds. Please try again.");
                Thread.Sleep(intervalInSec * 1000);
                build = GetBuild(project,build.Id.ToInt32());
            }
            return build;
        }

        public static int QueueBuild(string projectName, string buildName, string parameters=null)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {

                var projectClient = connection.GetClientAsync<ProjectHttpClient>().Result;
                var projects = projectClient.GetProjects(ProjectState.WellFormed).Result;
                var project = projects?.FirstOrDefault(p => p.Name.EqualsI(projectName));
                if (project==null)throw new ArgumentException($"Project '{projectName}' does not exist");

                var buildClient = connection.GetClientAsync<BuildHttpClient>().Result;
                var sourceBuildRefs = buildClient.GetDefinitionsAsync(project.Id).Result;
                var sourceBuildRef = sourceBuildRefs?.FirstOrDefault(b => b.Name.EqualsI(SourceBuildName));
                if (sourceBuildRef == null) throw new ArgumentException($"Build '{buildName}' does not exist");

                var newBuild = new Microsoft.TeamFoundation.Build.WebApi.Build
                {
                    Definition = new DefinitionReference
                    {
                        Id = sourceBuildRef.Id
                    },
                    Project = sourceBuildRef.Project
                };

                //Set the parameters
                if (!string.IsNullOrWhiteSpace(parameters)) newBuild.Parameters = parameters;

                // Build class has many properties, hoqever we can set only these properties.
                //ref: https://www.visualstudio.com/integrate/api/build/builds#queueabuild
                //In this nuget librari, we should set Project property.
                //It requires project's GUID, so we're compelled to get GUID by API.
                var res = buildClient.QueueBuildAsync(newBuild).Result;

                return res.Id;
            }
        }

        public static void AssignLicenceToIdentity(Guid identityId, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var licensingClient = connection.GetClient<LicensingHttpClient>();

                License licence = AccountLicense.Express;
                switch (licenceType)
                {
                    case LicenceTypes.Stakeholder:
                        licence = AccountLicense.Stakeholder;
                        break;
                    case LicenceTypes.Professional:
                        licence = AccountLicense.Professional;
                        break;
                    case LicenceTypes.Advanced:
                        licence = AccountLicense.Advanced;
                        break;
                    case LicenceTypes.Msdn:
                        licence = MsdnLicense.Eligible;
                        break;
                }
                var entitlement = licensingClient.AssignEntitlementAsync(identityId, licence).Result;
            }
        }

        public static LicenceTypes? GetIdentityLicence(Guid identityId, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var licensingClient = connection.GetClient<LicensingHttpClient>();

                var entitlement = licensingClient.GetAccountEntitlementAsync(identityId).Result;
                if (entitlement == null) return null;

                if (entitlement.License == AccountLicense.Stakeholder) return LicenceTypes.Stakeholder;
                if (entitlement.License == AccountLicense.Professional) return LicenceTypes.Professional;
                if (entitlement.License == AccountLicense.Advanced) return LicenceTypes.Advanced;
                if (entitlement.License == AccountLicense.Express) return LicenceTypes.Basic;
                if (entitlement.License == MsdnLicense.Eligible) return LicenceTypes.Msdn;
                if (entitlement.License.IsAny(MsdnLicense.Eligible, MsdnLicense.Enterprise, MsdnLicense.Platforms, MsdnLicense.Premium, MsdnLicense.Professional, MsdnLicense.TestProfessional, MsdnLicense.Ultimate)) return LicenceTypes.Msdn;
                throw new Exception($"Unknown license type '{entitlement.License}' for user identity '{identityId}'");
            }
        }

        public static void AddUserToAccount(string accountName, string userEmail, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            try
            {
                using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
                {

                    // We need the clients for two services: Licensing and Identity
                    var licensingClient = connection.GetClient<LicensingHttpClient>();
                    var identityClient = connection.GetClient<IdentityHttpClient>();

                    // The first call is to see if the user already exists in the account.
                    // Since this is the first call to the service, this will trigger the sign-in window to pop up.
                    Console.WriteLine("Sign in as the admin of account {0}. You will see a sign-in window on the desktop.", accountName);
                    var userIdentity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();

                    // If the identity is null, this is a user that has not yet been added to the account.
                    // We'll need to add the user as a "bind pending" - meaning that the email address of the identity is 
                    // recorded so that the user can log into the account, but the rest of the details of the identity 
                    // won't be filled in until first login.
                    if (userIdentity == null)
                    {
                        Console.WriteLine("Creating a new identity and adding it to the collection's licensed users group.");

                        // We are adding the user to a collection, and at the moment only one collection is supported per
                        // account in VSTS.
                        var collectionScope = identityClient.GetScopeAsync(accountName).Result;

                        // First get the descriptor for the licensed users group, which is a well known (built in) group.
                        var licensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType, GroupWellKnownSidConstants.LicensedUsersGroupSid);

                        // Now convert that into the licensed users group descriptor into a collection scope identifier.
                        var identifier = String.Concat(SidIdentityHelper.GetDomainSid(collectionScope.Id), SidIdentityHelper.WellKnownSidType, licensedUsersGroupDescriptor.Identifier.Substring(SidIdentityHelper.WellKnownSidPrefix.Length));

                        // Here we take the string representation and create the strongly-type descriptor
                        var collectionLicensedUsersGroupDescriptor = new IdentityDescriptor(IdentityConstants.TeamFoundationType, identifier);


                        // Get the domain from the user that runs this code. This domain will then be used to construct
                        // the bind-pending identity. The domain is either going to be "Windows Live ID" or the Azure 
                        // Active Directory (AAD) unique identifier, depending on whether the account is connected to
                        // an AAD tenant. Then we'll format this as a UPN string.
                        var currUserIdentity = connection.AuthorizedIdentity.Descriptor;
                        var directory = "Windows Live ID"; // default to an MSA (fka Live ID)
                        if (currUserIdentity.Identifier.Contains('\\'))
                        {
                            // The identifier is domain\userEmailAddress, which is used by AAD-backed accounts.
                            // We'll extract the domain from the admin user.
                            directory = currUserIdentity.Identifier.Split(new char[] { '\\' })[0];
                        }
                        var upnIdentity = string.Format("upn:{0}\\{1}", directory, userEmail);

                        // Next we'll create the identity descriptor for a new "bind pending" user identity.
                        var newUserDesciptor = new IdentityDescriptor(IdentityConstants.BindPendingIdentityType, upnIdentity);

                        // We are ready to actually create the "bind pending" identity entry. First we have to add the
                        // identity to the collection's licensed users group. Then we'll retrieve the Identity object
                        // for this newly-added user. Without being added to the licensed users group, the identity 
                        // can't exist in the account.
                        bool result = identityClient.AddMemberToGroupAsync(collectionLicensedUsersGroupDescriptor, newUserDesciptor).Result;
                        userIdentity = identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();
                    }

                    Console.WriteLine("Assigning license to user.");

                    License licence = AccountLicense.Express;
                    switch (licenceType)
                    {
                        case LicenceTypes.Stakeholder:
                            licence = AccountLicense.Stakeholder;
                            break;
                        case LicenceTypes.Professional:
                            licence = AccountLicense.Professional;
                            break;
                        case LicenceTypes.Advanced:
                            licence = AccountLicense.Advanced;
                            break;
                        case LicenceTypes.Msdn:
                            licence = MsdnLicense.Eligible;
                            break;
                    }
                    var entitlement = licensingClient.AssignEntitlementAsync(userIdentity.Id, licence).Result;
                }
                Console.WriteLine("Success!");
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\nSomething went wrong...");
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                }
            }
        }
        #endregion

        #region Projects
        public static List<Project> GetProjects()
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/_apis/projects/?api-version={ApiVersion}&stateFilter=All", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Project>();
            foreach (var item in json.value)
            {
                results.Add(new Project()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    Description = (string)item.description,
                    State = (string)item.state,
                    Url = (string)item.url
                });
            }
            return results;
        }

        public static Project GetProject(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/_apis/projects/{projectId}?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var result=new Project()
            {
                Id = (string)json.id,
                Name = (string)json.name,
                Description = (string)json.description,
                State = (string)json.state,
                Url = (string)json.url
            };
            result.Links = new Dictionary<string, string>();
            foreach(var link in json._links)
            {
                var key = (string)link.Name;
                var value = (string)link.Value.href;
                result.Links[key] = value;
            }
            return result;
        }

        public static Project CreateProject(string name, string description = null, string targetProcessTemplate=null)
        {
            if (string.IsNullOrWhiteSpace(targetProcessTemplate))
            {
                var processes = GetProcesses();
                targetProcessTemplate = processes.FirstOrDefault(p => p.IsDefault.ToBoolean())?.Id;
                if (string.IsNullOrWhiteSpace(targetProcessTemplate)) targetProcessTemplate = processes.FirstOrDefault()?.Id;
            }

             var body = new
            {
                name,
                description,
                capabilities = new
                {
                    versioncontrol = new
                    {
                        sourceControlType = "Git"
                    },
                    processTemplate = new
                    {
                        templateTypeId = targetProcessTemplate
                    }
                }
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{SourceCollectionUrl}/_apis/projects/?api-version={ApiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            var projectId=(string)json.id;

            DateTime expiration = DateTime.Now.AddSeconds(maxOpTimeInSeconds);
            Project project = null;
            while (true)
            {
                
                try
                {
                    project = GetProject(name);
                }
                catch (Exception ex)
                {
                    //404 occurs immediatly as project can be found
                    if (ex.InnerException==null || !(ex.InnerException is HttpRequestException) || !ex.InnerException.Message.ContainsI("404"))throw;
                }
                if (project!=null && project.State.EqualsI("WellFormed")) return project;
                if (DateTime.Now > expiration)throw new Exception(String.Format($"Operation {nameof(CreateProject)} did not complete in {maxOpTimeInSeconds} seconds. Please try again."));
                Thread.Sleep(intervalInSec * 1000);
            }
        }

        public static string EditProject(string projectId, string name=null, string description = null)
        {
            throw new NotImplementedException("This keeps returning 500 Internal error");

            var apiVersion = "2.0-preview";

            var body = new
            {
                name,
                description
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{SourceCollectionUrl}/_apis/projects/{projectId}?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.id;
        }

        private static async Task<Operation> WaitForLongRunningOperation(VssConnection connection, Guid operationId, int interavalInSec = 5, int maxTimeInSeconds = 60, CancellationToken cancellationToken = default(CancellationToken))
        {
            throw new NotImplementedException("This keeps hanging on GetOperation");
            OperationsHttpClient operationsClient = connection.GetClient<OperationsHttpClient>();
            DateTime expiration = DateTime.Now.AddSeconds(maxTimeInSeconds);
            while (true)
            {
                Operation operation = await operationsClient.GetOperation(operationId, cancellationToken);

                if (!operation.Completed)
                {
                    await Task.Delay(interavalInSec * 1000);

                    if (DateTime.Now > expiration)
                    {
                        throw new Exception(String.Format("Operation did not complete in {0} seconds.", maxTimeInSeconds));
                    }
                }
                else
                {
                    return operation;
                }
            }
        }
     
        public static bool EditProject(Project project, string name=null, string description=null)
        {
            if (project.Name == name && project.Description == description) return true;
            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException();

            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var client = connection.GetClient<ProjectHttpClient>();
                TeamProject updatedProject = new TeamProject();

                if (!string.IsNullOrWhiteSpace(name) && name!=project.Name) updatedProject.Name = name;
                if (!string.IsNullOrWhiteSpace(description) && description!=project.Description) updatedProject.Description = description;

                // Queue the update operation
                Guid updateOperationId = client.UpdateProject(new Guid(project.Id), updatedProject).Result.Id;

                // Check the operation status every 2 seconds (for up to 30 seconds)
                //Operation detailedUpdateOperation = WaitForLongRunningOperation(connection, updateOperationId, 2, 30).Result;

                return true; //detailedUpdateOperation.Status == OperationStatus.Succeeded;
            }
        }

        public static string DeleteProject(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Delete, $"{SourceCollectionUrl}/_apis/projects/{projectId}?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            return json.id;
        }

        public static Dictionary<string, string> GetProjectProperties(string projectId, params string[]keys)
        {
            var apiVersion = "4.0-preview";
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceInstanceUrl}/_apis/projects/{projectId}/properties?{(keys==null || keys.Length==0 ? "" : $"keys={keys.ToDelimitedString()}&")}api-version={apiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new Dictionary<string,string>();
            foreach (var item in json.value)
            {
                results[(string)item.name]= (string)item.value;
            }
            return results;
        }

        public static void SetProjectProperties(string projectId, Dictionary<string,string> properties)
        {
            var apiVersion = "4.0-preview";

            var body = new List<object>();
            foreach(var key in properties.Keys)
            {
                body.Add(new
                {
                    op="add",
                    path=$"/{key}",
                    value=properties[key]
                });
            }
            Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{SourceInstanceUrl}/_apis/projects/{projectId}/properties?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body));
        }

        public static string DeleteProjectProperties(string projectId, params string[] keys)
        {
            var apiVersion = "4.0-preview";

            var body = new List<object>();
            foreach (var key in keys)
            {
                body.Add(new
                {
                    op = "remove",
                    path = $"/{key}",
                });
            }
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{SourceInstanceUrl}/_apis/projects/{projectId}/properties?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.id;
        }

        #endregion

        #region Teams
        public static List<Team> GetTeams(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/_apis/projects/{projectId}/teams?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Team>();
            foreach (var item in json.value)
            {
                results.Add(new Team()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    Description = (string)item.description,
                    Url = (string)item.url,
                    IdentityUrl = (string)item.identityUrl
                });
            }
            return results;
        }

        public static List<Member> GetMembers(string projectId, string teamId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/_apis/projects/{projectId}/teams/{teamId}/members?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Member>();
            foreach (var item in json.value)
            {
                results.Add(new Member()
                {
                    Id = (string)item.id,
                    DisplayName = (string)item.displayName,
                    EmailAddress = (string)item.uniqueName,
                    Url = (string)item.url,
                    ImageUrl = (string)item.imageUrl
                });
            }
            return results;
        }

        public static bool RemoveUserFromTeam(string projectId, string teamId, string userEmail)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var client = connection.GetClient<IdentityHttpClient>();
                IdentitiesCollection identities = Task.Run(async () => await client.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, userEmail)).Result;

                if (!identities.Any() || identities.Count > 1) throw new InvalidOperationException("User not found or could not get an exact match based on email");

                var userIdentity = identities.Single();
                var groupIdentity = Task.Run(async () => await client.ReadIdentityAsync(teamId)).Result;
                return Task.Run(async () => await client.RemoveMemberFromGroupAsync(groupIdentity.Descriptor, userIdentity.Descriptor)).Result;
            }
        }

        public static void AddUserToTeam(string userEmail, string teamId)
        {
            using (var connection = GetConnection(SourceCollectionUrl, VSTSPersonalAccessToken))
            {
                var client = connection.GetClient<IdentityHttpClient>();
                IdentitiesCollection identities = Task.Run(async () => await client.ReadIdentitiesAsync(IdentitySearchFilter.MailAddress, userEmail)).Result;

                if (!identities.Any() || identities.Count > 1) throw new InvalidOperationException("User not found or could not get an exact match based on email");

                var userIdentity = identities.Single();
                var groupIdentity = Task.Run(async () => await client.ReadIdentityAsync(teamId)).Result;
                var success = Task.Run(async () => await client.AddMemberToGroupAsync(groupIdentity.Descriptor, userIdentity.Id)).Result;
            }
        }

        #endregion

        #region Repositories
        public static List<Repo> GetRepos(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{projectId}/_apis/git/repositories?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Repo>();
            foreach (var item in json.value)
            {
                results.Add(new Repo()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    DefaultBranch = (string)item.defaultBranch,
                    Url = (string)item.url,
                    RemoteUrl = (string)item.remoteUrl,
                    Project = new Project()
                    {
                        Id = (string)item.project.id,
                        Name = (string)item.project.name,
                        Description = (string)item.project.description,
                        State = (string)item.project.state,
                        Url = (string)item.project.url
                    }
                });
            }
            return results;
        }

        public static string CreateRepo(string projectId, string name)
        {
            var body = new
            {
                name,
                project = new {id=projectId }
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{SourceCollectionUrl}/{projectId}/_apis/git/repositories?api-version={ApiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.id;
        }
        public static string ImportRepo(string projectId, string repoId, string sourceUrl, string serviceEndpointId)
        {
            const string apiVersion = "3.0-preview";

            var body = new
            {
                parameters = new
                {
                    gitSource = new
                    {
                        url = sourceUrl
                    },
                    serviceEndpointId,
                    deleteServiceEndpointAfterImportIsDone = true
                }
            };
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{SourceCollectionUrl}/{projectId}/_apis/git/repositories/{repoId}/importRequests?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.importRequestId;
        }
        #endregion

        #region Build
        //public static string CreateBuildDefinition(string name, string projectId, string repositoryId = null)
        //{
        //    var apiVersion = "2.0";
        //    var buildAgent = "Hosted Linux Preview";

        //    var body=new BuildDefinition1()
        //    {
        //        Name = "myDefinition",
        //        Type = "build",
        //        Quality = "definition",
        //        Queue = new Queue
        //        {
        //            Id = 1
        //        },
        //        Build = new Build[]
        //        {
        //            new Build
        //            { Enabled=true},
        //            new Build
        //            { Enabled=true}
        //        },
        //        {
        //            Enabled=true,
        //            ContinueOnError=false,
        //            AlwaysRun=false,
        //            DisplayName="Build solution **\\*.sln",
        //            Task=new Task
        //            {
        //                Id="71a9a2d3-a98a-4caa-96ab-affca411ecda",
        //                VersionSpec="*"
        //            },

        //            Inputs=new Dictionary<string, string>
        //            {
        //                {"solution","**\\*.sln"},
        //                {"msbuildArgs",""},
        //                {"platform","$(platform)"},
        //                {"configuration","$(config)"},
        //                {"clean","false"},
        //                {"restoreNugetPackages","true" },
        //                ("vsLocationMethod","version"},
        //                {"vsVersion", "latest" },
        //                { "vsLocation" = "" },
        //                { "msbuildLocationMethod"="version",
        //                msbuildVersion="latest",
        //                msbuildArchitecture="x86",
        //                msbuildLocation="",
        //                logProjectEvents=true
        //            }
        //        },
        //        new {
        //            enabled=true,
        //            continueOnError=false,
        //            alwaysRun=false,
        //            displayName="Test Assemblies **\\*test*.dll;-:**\\obj\\**",
        //            task=new {
        //                id="ef087383-ee5e-42c7-9a53-ab56c98420f9",
        //                versionSpec="*"
        //            },
        //            inputs=new {
        //                testAssembly="**\\*test*.dll;-:**\\obj\\**",
        //                testFiltercriteria="",
        //                runSettingsFile="",
        //                codeCoverageEnabled="true",
        //                otherConsoleOptions="",
        //                vsTestVersion="14.0",
        //                pathtoCustomTestAdapters=""
        //            }
        //        }
        //    ]
        //    };

        //    var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Post, $"{CollectionUrl}/{projectId}/_apis/build/definitions?api-version={ApiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
        //    return json.id;
        //}



        #endregion

        #region Endpoints

        public static List<Endpoint> GetEndpoints(string project)
        {
            var apiVersion = "3.0-preview.1";
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/distributedtask/serviceendpoints?api-version={apiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Endpoint>();
            foreach (var item in json.value)
            {
                var result = new Endpoint()
                {
                    Id = (string)item.id,
                    Name = (string)item.name,
                    Type = (string)item.type,
                    IsReady = (string)item.isReady,
                    Authorization = item.authorization.parameters != null ? null : new Authorization()
                    {
                        Scheme = item.authorization.scheme
                    },
                    Url = (string)item.url
                };

                if (result.Authorization != null && item.authorization.parameters != null)
                {
                    result.Authorization.Parameters = new Dictionary<string, string>();
                    foreach (var link in item.authorization.parameters)
                    {
                        var key = (string)link.Name;
                        var value = (string)link.Value;
                        result.Authorization.Parameters[key] = value;
                    }
                }
                results.Add(result);
            }
            return results;
        }

        public static Endpoint GetEndpoint(string project, string endpointId)
        {
            var apiVersion = "3.0-preview.1";

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{SourceCollectionUrl}/{project}/_apis/distributedtask/serviceendpoints/{endpointId}?api-version={apiVersion}", password: VSTSPersonalAccessToken)).Result;
            if (json == null) return null;
            var result = new Endpoint()
            {
                Id = (string)json.id,
                Name = (string)json.name,
                Type = (string)json.type,
                IsReady = (string)json.isReady,
                Authorization = json.authorization.parameters == null ? null : new Authorization()
                {
                    Scheme = json.authorization.scheme
                },
                Url = (string)json.url
            };

            if (result.Authorization!=null && json.authorization.parameters != null)
            {
                result.Authorization.Parameters = new Dictionary<string, string>();
                foreach (var link in json.authorization.parameters)
                {
                    var key = (string)link.Name;
                    var value = (string)link.Value;
                    result.Authorization.Parameters[key] = value;
                }
            }
            return result;
        }

        public static Endpoint CreateEndpoint(string project, string name, string type, string url, string scheme, Dictionary<string,string> parameters)
        {
            const string apiVersion = "3.0-preview.1";

            var body = new
            {
                name,
                type,
                url,
                authorization = new
                {
                    scheme,
                    parameters,
                }
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{SourceCollectionUrl}/{project}/_apis/distributedtask/serviceendpoints?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            var result = new Endpoint()
            {
                Id = (string)json.id,
                Name = (string)json.name,
                Type = (string)json.type,
                IsReady = (string)json.isReady,
                Authorization = new Authorization()
                {
                    Scheme = json.authorization.scheme
                },
                Url = (string)json.url
            };

            result.Authorization.Parameters = new Dictionary<string, string>();
            foreach (var link in json.authorization.parameters)
            {
                var key = (string)link.Name;
                var value = (string)link.Value;
                result.Authorization.Parameters[key] = value;
            }
            return result;
        }

        #endregion


    }
}
