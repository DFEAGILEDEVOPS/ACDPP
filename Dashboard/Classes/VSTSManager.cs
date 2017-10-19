using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using Extensions;

namespace Dashboard.Classes
{
    public class VSTSManager
    {
        public const string Account = "agilefactory";
        const string ApiVersion = "1.0";
        const string ProjectTemplateId = "24a1e994-d40e-4e78-804d-8fa89c4e6c1d";

        #region Classes
        public class Project
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string State { get; set; }
            public string Url { get; set; }
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


        public static List<Project> GetProjects()
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Get, $"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/?api-version={ApiVersion}&stateFilter=All", password: AppSettings.VSTSPersonalAccessToken)).Result;
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

        public static string CreateProject(string name, string description=null)
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

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Post, $"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/?api-version={ApiVersion}", password: AppSettings.VSTSPersonalAccessToken,body:body)).Result;
            return json.id;
        }

        public static string DeleteProject(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Delete, $"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}?api-version={ApiVersion}", password: AppSettings.VSTSPersonalAccessToken)).Result;
            return json.id;
        }

        public static List<Team> GetTeams(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Get, $"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}/teams?api-version={ApiVersion}", password: AppSettings.VSTSPersonalAccessToken)).Result;
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Get, $"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}/teams/{teamId}/members?api-version={ApiVersion}", password: AppSettings.VSTSPersonalAccessToken)).Result;
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

        public static List<Repo> GetRepos(string projectId)
        {
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Get, $"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/git/repositories?api-version={ApiVersion}", password: AppSettings.VSTSPersonalAccessToken)).Result;
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

            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Post, $"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/distributedtask/serviceendpoints?api-version={apiVersion}", password: AppSettings.VSTSPersonalAccessToken, body: body)).Result;
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
            var json = Task.Run(async () => await Web.CallJsonApiAsync(HttpMethod.Post, $"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/git/repositories/{repoId}/importRequests?api-version={apiVersion}", password: AppSettings.VSTSPersonalAccessToken, body: body)).Result;
            return json.importRequestId;
        }

    }
}
