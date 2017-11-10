using System;
using System.Collections.Generic;
using System.Linq;
using Dashboard.Models;
using Dashboard.Classes;
using Extensions;
using System.Net;
using System.Web.Mvc;
using Dashboard.Net.Models;
using VstsApi.Net;
using System.Collections.Specialized;
using System.Diagnostics;
using Extensions.Net.Classes;
using Dashboard.Net.Classes;
using Newtonsoft.Json;

namespace Dashboard.Controllers
{
    [BasicAuthentication()]
    [RoutePrefix("projects")]
    [Route("{action}")]
    public class ProjectsController : Controller
    {

        [Route]
        [HttpGet]
        public ActionResult Index()
        {
            var model = new ProjectsViewModel();
            var projects = VstsManager.GetProjects();
            foreach (var project in projects.OrderBy(p=>p.Name))
            {
                if (!project.State.EqualsI("WellFormed","CreatePending","New")) continue;
                var newProject = new ProjectViewModel();
                newProject.Properties = VstsManager.GetProjectProperties(project.Id, ProjectProperties.All);

                newProject.Id = project.Id;
                newProject.Name = project.Name;
                newProject.Description = project.Description;
                newProject.CostCode = newProject.Properties[ProjectProperties.CostCode];
                if (newProject.Properties[ProjectProperties.CreatedBy] == AppSettings.ProjectCreatedBy)
                {
                    var p = VstsManager.GetProject(newProject.Id);
                    newProject.Url = p.Links["web"];
                    newProject.AppUrl = newProject.Properties[ProjectProperties.AppUrl];
                }

                model.Add(newProject);
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new ProjectViewModel();
            model.CostCode = Crypto.GeneratePasscode("0123456789".ToCharArray(), 8);
            int c = 1;
            string name = $"TestProject{c}";
            var projects = VstsManager.GetProjects();
            while (projects.Any(p => p.Name.Equals(name)))
            {
                name = $"TestProject{++c}";
            }
            model.Name = name;
            model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel()};
            return View("Project",model);
        }

        [HttpPost]
        public ActionResult Create(ProjectViewModel model,string command)
        {
            try
            {
                if (command == "Add")
                {
                    model.TeamMembers.Add(new TeamMemberViewModel());
                    ModelState.Clear();
                }
                else if (command.StartsWithI("Remove"))
                {
                    var i = command.AfterFirst(":").ToInt32(-1);
                    if (i > -1) model.TeamMembers.RemoveAt(i);
                    if (model.TeamMembers.Count<1)model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel() };

                    ModelState.Clear();
                }
                else if (command.EqualsI("Create Project","Save Changes"))
                {
                    #region Step 1: Validation
                    int c = 0;
                    foreach (var member in model.TeamMembers)
                    {
                        if (string.IsNullOrWhiteSpace(member.Name) && string.IsNullOrWhiteSpace(member.Email)) continue;
                        c++;
                    }
                    if (c == 0)ModelState.AddModelError("", "You must enter at least one team member");
                    if (!ModelState.IsValid)return View("Project",model);

                    //Check project name doesnt already exist
                    var allProjects = VstsManager.GetProjects();

                    //Get the target project
                    var targetProject= !string.IsNullOrWhiteSpace(model.Id) ? allProjects.FirstOrDefault(p => p.Id == model.Id) : allProjects.FirstOrDefault(p => p.Name.EqualsI(model.Name));
                    if (targetProject != null)
                    {
                        targetProject = VstsManager.GetProject(targetProject.Id); //Get the links etc
                        if (string.IsNullOrWhiteSpace(model.Id))model.Id = targetProject.Id;
                    }

                    //Check the target doesnt already exist
                    if (allProjects.Any(p => p.Name.ToLower() == model.Name.ToLower() && (targetProject==null || p.Id!=targetProject.Id)))
                    {
                        ModelState.AddModelError(nameof(model.Name), "A project with this name already exists");
                        return View("Project",model);
                    }

                    //Get the source project
                    var sourceProject = allProjects.FirstOrDefault(p => p.Name.EqualsI(AppSettings.SourceProjectName));
                    #endregion

                    #region Step 2: Create the project
                    //Create the build parameters
                    var parameters = new Dictionary<string, string>();
                    parameters["oc_project_name"] = model.Name.ToLower().ReplaceI("_", "-").ReplaceI(" ");
                    parameters["oc_build_config_name"] = $"webbapp";
                    var jsonParameters = JsonConvert.SerializeObject(parameters);
                    var appUrl = $"https://{parameters["oc_build_config_name"]}-{parameters["oc_project_name"]}.demo.dfe.secnix.co.uk/";

                    string newProjectId = null;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(model.Id))
                        {
                            targetProject = VstsManager.CreateProject(model.Name, model.Description);
                            model.Id = targetProject.Id;
                            newProjectId = targetProject.Id;
                        }
                        else if (!model.Name.Equals(targetProject.Name) || model.Description!=targetProject.Description)
                        {
                            if (!VstsManager.EditProject(targetProject, model.Name, model.Description)) throw new Exception("Could not update project name or description");
                        }
                        if (!targetProject.State.EqualsI("WellFormed")) throw new Exception("Project has not yet completed creation. Please try again");

                        //Add the special properties
                        var oldProperties = VstsManager.GetProjectProperties(model.Id, ProjectProperties.All);
                        var newProperties = new NameValueCollection();
                        if (!oldProperties.ContainsKey(ProjectProperties.CreatedBy)) newProperties[ProjectProperties.CreatedBy] = AppSettings.ProjectCreatedBy;
                        if (!oldProperties.ContainsKey(ProjectProperties.CreatedDate)) newProperties[ProjectProperties.CreatedDate] = DateTime.Now.ToString();
                        if (!oldProperties.ContainsKey(ProjectProperties.CostCode)) newProperties[ProjectProperties.CostCode] = model.CostCode;
                        if (!oldProperties.ContainsKey(ProjectProperties.AppUrl)) newProperties[ProjectProperties.AppUrl] = appUrl;
                        if (newProperties.Count > 0) VstsManager.SetProjectProperties(model.Id, newProperties);
                        //Marke the project as created properly
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
                    var repos = VstsManager.GetRepos(model.Id);
                    var repo = repos.Count==0 ? null : repos[0];

                    //Create a new repo
                    if (repo == null)
                    {
                        var repoId = VstsManager.CreateRepo(model.Id, targetProject.Name);
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
                        VstsManager.ImportRepo(model.Id, repo.Id, AppSettings.SourceRepoUrl, serviceEndpoint.ToString());
                    }
                    #endregion

                    #region Step 4: Build and deploy the OpenShift environment

                    //Get the build definition
                    var definitions = VstsManager.GetDefinitions(sourceProject.Id, AppSettings.ConfigBuildName);
                    var sourceDefinition = definitions.FirstOrDefault(d => d.Name.EqualsI(AppSettings.ConfigBuildName));

                    //Get the latest build 
                    var builds = VstsManager.GetBuilds(sourceProject.Id, sourceDefinition.Id).Where(b=>b.Parameters.EqualsI(jsonParameters));
                    var build = builds.OrderByDescending(b => b.QueueTime).FirstOrDefault();

                    //Create a new build if the last failed
                    if (build == null || (build.Status.EqualsI("Completed") && build.Result.EqualsI("failed")))build = VstsManager.QueueBuild(sourceProject.Id.ToGuid(), sourceDefinition.Id.ToInt32(), jsonParameters);

                    //Wait for the build to finish
                    if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(sourceProject.Id, build);

                    //Ensure the build succeeded
                    if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
                    #endregion

                    #region Step 5: Copy the source build
                    //Clone the sample build definition from the source to target project
                    sourceDefinition = VstsManager.GetDefinitions(targetProject.Id, AppSettings.SourceBuildName).FirstOrDefault();
                    if (sourceDefinition==null) sourceDefinition = VstsManager.CloneDefinition(AppSettings.SourceProjectName, AppSettings.SourceBuildName,targetProject.Name,repo, parameters,true);

                    //Get the latest build 
                    builds = VstsManager.GetBuilds(targetProject.Id, sourceDefinition.Id).Where(b => b.Parameters.EqualsI(jsonParameters));
                    build = builds.OrderByDescending(b => b.QueueTime).FirstOrDefault();
                    #endregion

                    #region Step 6: Build and deploy the sample application
                    //Create a new build if the last failed
                    if (build == null || (build.Status.EqualsI("Completed") && build.Result.EqualsI("failed"))) build = VstsManager.QueueBuild(targetProject.Id.ToGuid(), sourceDefinition.Id.ToInt32(), jsonParameters);

                    //Wait for the build to finish
                    if (!build.Status.EqualsI("Completed")) build = VstsManager.WaitForBuild(sourceProject.Id, build);
                    
                    //Ensure the build succeeded
                    if (!build.Result.EqualsI("succeeded")) throw new Exception($"Build {build.Result}: '{build.Definition.Name}:{build.Id}' ");
                    #endregion

                    #region Step 7: Update the team members
                    var teams = VstsManager.GetTeams(model.Id);
                    var team = teams[0];
                    var members=VstsManager.GetMembers(model.Id, team.Id);

                    foreach (var member in model.TeamMembers)
                    {
                        //Ensure the user account exists
                        var identity = VstsManager.GetAccountUserIdentity(VstsManager.SourceAccountName,member.Email);
                        if (identity == null)identity=VstsManager.CreateAccountIdentity(VstsManager.SourceAccountName, member.Email);
                        var licence=VstsManager.GetIdentityLicence(identity);

                        //Add or change the licence type
                        if (!member.Email.EqualsI(AppSettings.VSTSAccountEmail) && licence == null || (licence.Value != member.LicenceType && !licence.Value.IsAny(VstsManager.LicenceTypes.Msdn, VstsManager.LicenceTypes.Advanced, VstsManager.LicenceTypes.Professional)))
                            VstsManager.AssignLicenceToIdentity(identity, member.LicenceType);

                        //Ensure the user is a member of the team
                        if (!members.Any(m => m.UniqueName.EqualsI(member.Email, AppSettings.VSTSAccountEmail)))
                            VstsManager.AddUserToTeam(member.Email, team.Id);
                    }
                    //Get the new team members
                    var newMembers = model.TeamMembers.Where(m => !members.Any(m1 => m1.UniqueName.EqualsI(m.Email))).ToList();

                    //Remove old users
                    foreach (var member in members)
                    {
                        if (member.UniqueName.EqualsI(AppSettings.VSTSAccountEmail))continue;

                        if (!model.TeamMembers.Any(m => m.Email.EqualsI(member.UniqueName)))
                            VstsManager.RemoveUserFromTeam(model.Id, team.Id, member.UniqueName);
                    }
                    #endregion

                    #region Step 8: Welcome the team members
                    if (newMembers.Count>0)
                    {
                        var projectUrl = targetProject.Links["web"];
                        var gitUrl = repo.RemoteUrl;

                        var notify = new GovNotifyAPI();
                        foreach (var member in newMembers)
                        {
                            var personalisation = new Dictionary<string, dynamic> { { "name", member.Name }, { "email", member.Email }, { "project", model.Name }, { "projecturl", projectUrl }, { "giturl", gitUrl }, { "appurl", appUrl } };
                            notify.SendEmail(member.Email, AppSettings.WelcomeTemplateId, personalisation);
                        }
                    }
                    #endregion

                    //Show success confirmation
                    return View("CustomError",new CustomErrorViewModel {  Title="Complete", Subtitle=$"Project successfully {(command=="Save Changes" ? "saved" : "created")}", Description=$"Your project was successfully {(command == "Save Changes" ? "saved" : "created and welcome emails sent to the team members")}.",  CallToAction="View projects...", ActionText="Continue", ActionUrl=Url.Action("Index")});
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", (ex.InnerException ?? ex).Message);
            }
            return View("Project",model);
        }

        [HttpGet]
        public ActionResult Edit(string Id)
        {
            //Check the project exists
            var project = VstsManager.GetProjects().FirstOrDefault(p=>p.Id==Id);
            if (project == null)return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to projects list...", ActionText = "Continue", ActionUrl = Url.Action("Index") });

            //Load the project 
            var model = new ProjectViewModel();
            model.Id = project.Id;
            model.Name = project.Name;
            model.Description = project.Description;
            var properties = VstsManager.GetProjectProperties(project.Id, ProjectProperties.CreatedBy, ProjectProperties.CreatedDate, ProjectProperties.CostCode);
            model.CostCode = properties[ProjectProperties.CostCode];

            var teams = VstsManager.GetTeams(project.Id);
            if (teams.Count>0)model.TeamMembers = VstsManager.GetMembers(project.Id,teams[0].Id).Select(m=>new TeamMemberViewModel() { TeamMemberId=m.Id, FirstName = m.DisplayName.BeforeFirst(" ").BeforeFirst("."), LastName = m.DisplayName.AfterFirst(" ").AfterFirst("."), Email = m.UniqueName }).ToList();
            if (model.TeamMembers.Count<1) model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel()};

            return View("Project",model);
        }

        [HttpPost]
        public ActionResult Edit(ProjectViewModel model, string command)
        {
            return Create(model, command);
        }


        [HttpGet]
        public ActionResult Delete(string Id)
        {
            //Check the project exists
            var project = VstsManager.GetProjects().FirstOrDefault(p => p.Id == Id);
            if (project == null) return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to projects list...", ActionText = "Continue", ActionUrl = Url.Action("Edit",new {Id}) });

            //Load the project 
            var model = new ProjectViewModel();
            model.Id = project.Id;
            model.Name = project.Name;
            model.Description = project.Description;
            var properties = VstsManager.GetProjectProperties(project.Id, ProjectProperties.CreatedBy, ProjectProperties.CreatedDate, ProjectProperties.CostCode);
            model.CostCode = properties[ProjectProperties.CostCode];

            var teams = VstsManager.GetTeams(project.Id);
            if (teams.Count > 0) model.TeamMembers = VstsManager.GetMembers(project.Id, teams[0].Id).Select(m => new TeamMemberViewModel() { TeamMemberId = m.Id, FirstName = m.DisplayName.BeforeFirst(" ").BeforeFirst("."), LastName = m.DisplayName.AfterFirst(" ").AfterFirst("."), Email = m.UniqueName }).ToList();
            if (model.TeamMembers.Count < 1) model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel() };

            return View("Delete", model);
        }

        [HttpPost]
        public ActionResult Delete(ProjectViewModel model, string projectName)
        {
            ModelState.Exclude(nameof(model.Name),nameof(model.CostCode));

            var project = VstsManager.GetProjects().FirstOrDefault(p => p.Id == model.Id);
            if (string.IsNullOrWhiteSpace(projectName) || !model.Name.EqualsI(projectName))
            {
                ModelState.AddModelError(nameof(model.Name), "You must enter the exact project name");
                return View(model);
            }

            if (project == null) return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });
            model.Properties = VstsManager.GetProjectProperties(model.Id, ProjectProperties.CreatedBy);
            if (model.Properties[ProjectProperties.CreatedBy] != AppSettings.ProjectCreatedBy) return View("CustomError", new CustomErrorViewModel { Title = "Unauthorised", Subtitle = "You dont have permission to delete this project", Description = "This project was not created here and cannot be deleted.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });

            //TODO Delete the project
            VstsManager.DeleteProject(model.Id);
            return View("CustomError", new CustomErrorViewModel { Title = "Complete", Subtitle = "Project successfully deleted", Description = $"Your project was successfully deleted.", CallToAction = "View projects...", ActionText = "Continue", ActionUrl = Url.Action("Index") });
        }
    }
}
