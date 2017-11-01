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
using System.Collections.Specialized;
using Extensions.Net;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Http;

namespace VstsApi.Net
{
    public class VstsManager
    {
        public const string Account = "agilefactory";
        const string ApiVersion = "1.0";
        const string ProjectTemplateId = "24a1e994-d40e-4e78-804d-8fa89c4e6c1d";
        public static string InstanceUrl = $"https://{Account}.visualstudio.com/";
        public static string CollectionUrl = $"{InstanceUrl}DefaultCollection";
        public static string VSTSPersonalAccessToken = ConfigurationManager.AppSettings["VSTSPersonalAccessToken"];
        private static readonly int intervalInSec=2;
        private static readonly int maxOpTimeInSeconds=60;

        #region Classes

        public enum LicenceTypes
        {
            Stakeholder=1,
            Basic=2,
            Professional=3,
            Advanced=4,
            Msdn=5
        }

        public class Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string State { get; set; }
            public string Url { get; set; }
            public Dictionary<string,string> Links { get; set; }
            public string DefaultTeamId { get; set; }
        }
        public class Team
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Url { get; set; }
            public string IdentityUrl { get; set; }
        }
        public class Member
        {
            public string Id { get; set; }
            public string DisplayName { get; set; }
            public string UniqueName { get; set; }
            public string Url { get; set; }
            public string ImageUrl { get; set; }
        }
        public class Repo
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string DefaultBranch { get; set; }
            public string Url { get; set; }
            public string RemoteUrl { get; set; }
            public Project Project { get; set; }
        }
        #endregion

        #region Accounts
        private static VssConnection GetConnection(string collectionUrl, string personalAccessToken)
        {
            return new VssConnection(new Uri(collectionUrl), new VssBasicCredential(string.Empty, personalAccessToken));
        }

        public static Identity GetAccountUserIdentity(string accountName, string userEmail)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
            {
                var identityClient = connection.GetClient<IdentityHttpClient>();
                return identityClient.ReadIdentitiesAsync(IdentitySearchFilter.AccountName, userEmail).Result.FirstOrDefault();                    
            }
        }
        public static IdentitiesCollection GetAccountUserIdentities(string accountName)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
            {
                var identityClient = connection.GetClient<IdentityHttpClient>();
                return identityClient.ReadIdentitiesAsync(IdentitySearchFilter.General,"").Result;
            }
        }

        public static Identity CreateAccountIdentity(string accountName, string userEmail)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
                return userIdentity;
            }
        }

        public static void AssignLicenceToIdentity(Identity userIdentity, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
                var entitlement = licensingClient.AssignEntitlementAsync(userIdentity.Id, licence).Result;
            }
        }

        public static LicenceTypes? GetIdentityLicence(Identity userIdentity, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
            {
                var licensingClient = connection.GetClient<LicensingHttpClient>();

                var entitlement = licensingClient.GetAccountEntitlementAsync(userIdentity.Id).Result;
                if (entitlement == null) return null;

                if (entitlement.License == AccountLicense.Stakeholder) return LicenceTypes.Stakeholder;
                if (entitlement.License == AccountLicense.Professional) return LicenceTypes.Professional;
                if (entitlement.License == AccountLicense.Advanced) return LicenceTypes.Advanced;
                if (entitlement.License == AccountLicense.Express) return LicenceTypes.Basic;
                if (entitlement.License == MsdnLicense.Eligible) return LicenceTypes.Msdn;
                if (entitlement.License.IsAny(MsdnLicense.Eligible, MsdnLicense.Enterprise, MsdnLicense.Platforms, MsdnLicense.Premium, MsdnLicense.Professional, MsdnLicense.TestProfessional, MsdnLicense.Ultimate)) return LicenceTypes.Msdn;
                throw new Exception($"Unknown license type '{entitlement.License}' for user identity '{userIdentity.DisplayName}'");
            }
        }

        public static void AddUserToAccount(string accountName, string userEmail, LicenceTypes licenceType = LicenceTypes.Basic)
        {
            try
            {
                using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{CollectionUrl}/_apis/projects/?api-version={ApiVersion}&stateFilter=All", password: VSTSPersonalAccessToken)).Result;
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{CollectionUrl}/_apis/projects/{projectId}?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
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

        public static Project CreateProject(string name, string description = null)
        {
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
                        templateTypeId = ProjectTemplateId
                    }
                }
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{CollectionUrl}/_apis/projects/?api-version={ApiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
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

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{CollectionUrl}/_apis/projects/{projectId}?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
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

            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Delete, $"{CollectionUrl}/_apis/projects/{projectId}?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            return json.id;
        }

        public static NameValueCollection GetProjectProperties(string projectId, params string[]keys)
        {
            var apiVersion = "4.0-preview";
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{InstanceUrl}/_apis/projects/{projectId}/properties?{(keys==null || keys.Length==0 ? "" : $"keys={keys.ToDelimitedString()}&")}api-version={apiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new NameValueCollection();
            foreach (var item in json.value)
            {
                results.Add((string)item.name, (string)item.value);
            }
            return results;
        }

        public static void SetProjectProperties(string projectId, NameValueCollection properties)
        {
            var apiVersion = "4.0-preview";

            var body = new List<object>();
            foreach(var key in properties.AllKeys)
            {
                body.Add(new
                {
                    op="add",
                    path=$"/{key}",
                    value=properties[key]
                });
            }
            Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{InstanceUrl}/_apis/projects/{projectId}/properties?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body));
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Patch, $"{InstanceUrl}/_apis/projects/{projectId}/properties?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.id;
        }

        #endregion

        #region Teams
        public static List<Team> GetTeams(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{CollectionUrl}/_apis/projects/{projectId}/teams?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{CollectionUrl}/_apis/projects/{projectId}/teams/{teamId}/members?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
            var results = new List<Member>();
            foreach (var item in json.value)
            {
                results.Add(new Member()
                {
                    Id = (string)item.id,
                    DisplayName = (string)item.displayName,
                    UniqueName = (string)item.uniqueName,
                    Url = (string)item.url,
                    ImageUrl = (string)item.imageUrl
                });
            }
            return results;
        }

        public static bool RemoveUserFromTeam(string projectId, string teamId, string userEmail)
        {
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
            using (var connection = GetConnection(CollectionUrl, VSTSPersonalAccessToken))
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Get, $"{CollectionUrl}/{projectId}/_apis/git/repositories?api-version={ApiVersion}", password: VSTSPersonalAccessToken)).Result;
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

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{CollectionUrl}/{projectId}/_apis/git/repositories?api-version={ApiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{CollectionUrl}/{projectId}/_apis/git/repositories/{repoId}/importRequests?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
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
        public static string CreateEndpoint(string projectId, string sourceUrl, string sourceUsername, string sourcePassword)
        {
            const string apiVersion = "3.0-preview.1";

            var body = new
            {
                name = "HelloWorld-Git-" + Guid.NewGuid(),
                type = "Git",
                url = sourceUrl,

                authorization = new
                {
                    scheme = "UsernamePassword",
                    parameters = new
                    {
                        username = sourceUsername,
                        password = sourcePassword
                    },
                }
            };

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethods.Post, $"{CollectionUrl}/{projectId}/_apis/distributedtask/serviceendpoints?api-version={apiVersion}", password: VSTSPersonalAccessToken, body: body)).Result;
            return json.id;
        }
        #endregion


    }
}
