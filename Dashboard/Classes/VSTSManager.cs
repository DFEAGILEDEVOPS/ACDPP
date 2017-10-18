using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;
using LibGit2Sharp;
using Extensions;
using LibGit2Sharp.Handlers;

namespace Dashboard.Classes
{
    public class VSTSManager
    {
        const string Account = "agilefactory";
        const string ApiVersion = "1.0";
        const string ProjectTemplateId = "24a1e994-d40e-4e78-804d-8fa89c4e6c1d";
        const string VSRepository = "https://agilefactory.visualstudio.com/_git/ACDPP";

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
            var json = Task.Run(async () => await GetProjectsAsync()).Result;
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

        public static async Task<dynamic> GetProjectsAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                using (HttpResponseMessage response = client.GetAsync($"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/?api-version={ApiVersion}&stateFilter=All").Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

        public static string CreateProject(string projectName)
        {
            var json = Task.Run(async () => await CreateProjectAsync(projectName)).Result;
            return json.id;
        }

        public static async Task<dynamic> CreateProjectAsync(string name, string description=null)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                var body = new
                {
                    name,
                    description,
                    capabilities=new
                    {
                        versioncontrol = new
                        {
                            sourceControlType="Git"
                        },
                        processTemplate = new
                        {
                            templateTypeId=ProjectTemplateId
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = client.PostAsync($"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/?api-version={ApiVersion}", httpContent).Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

        public static string DeleteProject(string projectId)
        {
            var json = Task.Run(async () => await DeleteProjectAsync(projectId)).Result;
            return json.id;
        }

        public static async Task<dynamic> DeleteProjectAsync(string projectId)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                using (HttpResponseMessage response = client.DeleteAsync($"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}?api-version={ApiVersion}").Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }


        public static List<Team> GetTeams(string projectId)
        {
            var json = Task.Run(async () => await GetTeamsAsync(projectId)).Result;
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

        public static async Task<dynamic> GetTeamsAsync(string projectId)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                using (HttpResponseMessage response = client.GetAsync($"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}/teams?api-version={ApiVersion}").Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

        public static List<Member> GetMembers(string projectId, string teamId)
        {
            var json = Task.Run(async () => await GetMembersAsync(projectId, teamId)).Result;
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

        public static async Task<dynamic> GetMembersAsync(string projectId, string teamId)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                using (HttpResponseMessage response = client.GetAsync($"https://{Account}.visualstudio.com/DefaultCollection/_apis/projects/{projectId}/teams/{teamId}/members?api-version={ApiVersion}").Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

        public static List<Repo> GetRepos(string projectId)
        {
            var json = Task.Run(async () => await GetReposAsync(projectId)).Result;
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

        public static async Task<dynamic> GetReposAsync(string projectId)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                using (HttpResponseMessage response = client.GetAsync($"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/git/repositories?api-version={ApiVersion}").Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }

        public static string CreateEndpoint(string projectId, string sourceUrl, string sourceUsername, string sourcePassword)
        {
            var json = Task.Run(async () => await CreateEndpointAsync(projectId, sourceUrl, sourceUsername, sourcePassword)).Result;
            return json.id;
        }

        public static async Task<dynamic> CreateEndpointAsync(string projectId, string sourceUrl, string sourceUsername, string sourcePassword)
        {
            const string apiVersion = "3.0-preview.1";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                var body = new
                {
                    name="HelloWorld-Git-"+Guid.NewGuid(),
                    type="Git",
                    url = sourceUrl,

                    authorization = new
                    {
                        scheme="UsernamePassword",
                        parameters = new
                        {
                            username=sourceUsername,
                            password=sourcePassword
                        },
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = client.PostAsync($"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/distributedtask/serviceendpoints?api-version={apiVersion}", httpContent).Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }
        public static string ImportRepo(string projectId, string repoId, string sourceUrl, string serviceEndpointId)
        {
            var json = Task.Run(async () => await ImportRepoAsync(projectId, repoId, sourceUrl,serviceEndpointId)).Result;
            return json.importRequestId;
        }

        public static async Task<dynamic> ImportRepoAsync(string projectId,string repoId, string sourceUrl,string serviceEndpointId)
        {
            const string apiVersion = "3.0-preview";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(string.Format("{0}:{1}", "", AppSettings.VSTSPersonalAccessToken))));

                var body = new
                {
                    parameters = new
                    {
                        gitSource = new
                        {
                            url = sourceUrl
                        },
                        serviceEndpointId,
                        deleteServiceEndpointAfterImportIsDone=true
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = client.PostAsync($"https://{Account}.visualstudio.com/DefaultCollection/{projectId}/_apis/git/repositories/{repoId}/importRequests?api-version={apiVersion}", httpContent).Result)
                {
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseBody);
                    return jsonResponse;
                }
            }
        }
        public static void CloneRepo(string sourceUrl, string sourceUsername, string sourcePassword, string targetUrl=null, string targetUsername=null, string targetPassword=null)
        {
            if (string.IsNullOrWhiteSpace(targetUrl)) targetUrl = sourceUrl;
            if (string.IsNullOrWhiteSpace(targetUsername)) targetUsername = sourceUsername;
            if (string.IsNullOrWhiteSpace(targetPassword)) targetPassword = sourcePassword;

             var cloneOptions = new CloneOptions() { CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = sourceUsername, Password = sourcePassword } };
            var pushOptions = new PushOptions() { CredentialsProvider = new CredentialsHandler((url, usernameFromUrl, types) => new UsernamePasswordCredentials() { Username = targetUsername, Password = targetPassword }) };


            using (var tmpDir = new TemporaryDirectory())
            {
                var path=Repository.Clone(sourceUrl, tmpDir.FullName, cloneOptions);

                using (var sourceRepo = new Repository(path))
                {
                    var remote=sourceRepo.Network.Remotes.Add("origin1", targetUrl);
                    sourceRepo.Network.Push(remote, @"refs/heads/master", pushOptions);
                }
            }
        }
    }
}
