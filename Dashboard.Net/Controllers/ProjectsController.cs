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
                    int c = 0;
                    foreach (var member in model.TeamMembers)
                    {
                        if (string.IsNullOrWhiteSpace(member.Name) && string.IsNullOrWhiteSpace(member.Email)) continue;
                        c++;
                    }
                    if (c == 0)ModelState.AddModelError("", "You must enter at least one team member");
                    if (!ModelState.IsValid)return View("Project",model);

                    //Check project name doesnt already exist
                    var projects = VstsManager.GetProjects();

                    var project= !string.IsNullOrWhiteSpace(model.Id) ? projects.FirstOrDefault(p => p.Id == model.Id) : projects.FirstOrDefault(p => p.Name.EqualsI(model.Name));
                    if (project != null)
                    {
                        project = VstsManager.GetProject(project.Id); //Get the links etc
                        if (string.IsNullOrWhiteSpace(model.Id))model.Id = project.Id;
                    }

                    if (projects.Any(p => p.Name.ToLower() == model.Name.ToLower() && (project==null || p.Id!=project.Id)))
                    {
                        ModelState.AddModelError(nameof(model.Name), "A project with this name already exists");
                        return View("Project",model);
                    }

                    //Create Project (if it doesnt already exist)

                    string newProjectId = null;
                    try
                    {
                        if (string.IsNullOrWhiteSpace(model.Id))
                        {
                            project = VstsManager.CreateProject(model.Name, model.Description);
                            model.Id = project.Id;
                            newProjectId = project.Id;
                        }
                        else if (!model.Name.Equals(project.Name) || model.Description!=project.Description)
                        {
                            if (!VstsManager.EditProject(project, model.Name, model.Description)) throw new Exception("Could not update project name or description");
                        }
                        if (!project.State.EqualsI("WellFormed")) throw new Exception("Project has not yet completed creation. Please try again");

                        //Add the special properties
                        var oldProperties = VstsManager.GetProjectProperties(model.Id, ProjectProperties.All);
                        var newProperties = new NameValueCollection();
                        if (!oldProperties.ContainsKey(ProjectProperties.CreatedBy)) newProperties[ProjectProperties.CreatedBy] = AppSettings.ProjectCreatedBy;
                        if (!oldProperties.ContainsKey(ProjectProperties.CreatedDate)) newProperties[ProjectProperties.CreatedDate] = DateTime.Now.ToString();
                        if (!oldProperties.ContainsKey(ProjectProperties.CostCode)) newProperties[ProjectProperties.CostCode] = model.CostCode;
                        if (newProperties.Count > 0) VstsManager.SetProjectProperties(model.Id, newProperties);
                        //Marke the project as created properly
                        newProjectId = null;
                    }
                    finally
                    {
                        //Delete the project if we couldnt create properly
                        if (!string.IsNullOrWhiteSpace(newProjectId)) VstsManager.DeleteProject(newProjectId);
                    }

                    //Get the Repo
                    var repos = VstsManager.GetRepos(model.Id);
                    var repo = repos.Count==0 ? null : repos[0];

                    //Create a new repo
                    if (repo == null)
                    {
                        var repoId = VstsManager.CreateRepo(model.Id, project.Name);
                        if (string.IsNullOrWhiteSpace(repoId)) throw new Exception($"Could not create repo '{project.Name}'");
                        repo = repos.Count == 0 ? null : repos[0];
                    }
                    if (repo == null) throw new Exception("Could not create repo '{project.Name}'");

                    //copy the sample repo if it doesnt already exist
                    if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
                    {
                        var serviceEndpointId = VstsManager.CreateEndpoint(model.Id, AppSettings.SourceRepoUrl, "", AppSettings.VSTSPersonalAccessToken);
                        VstsManager.ImportRepo(model.Id, repo.Id, AppSettings.SourceRepoUrl, serviceEndpointId);
                    }

                    //TODO Create the sample build definition passing in tproject arguments

                    //Get the team & members
                    var teams = VstsManager.GetTeams(model.Id);
                    var team = teams[0];
                    var members=VstsManager.GetMembers(model.Id, team.Id);

                    //Create the new users and add to team 
                    foreach (var member in model.TeamMembers)
                    {
                        //Ensure the user account exists
                        var identity = VstsManager.GetAccountUserIdentity(VstsManager.Account,member.Email);
                        if (identity == null)identity=VstsManager.CreateAccountIdentity(VstsManager.Account, member.Email);
                        var licence=VstsManager.GetIdentityLicence(identity);

                        //Add or change the licence type
                        if (!member.Email.EqualsI(AppSettings.CurrentUserEmail) && licence == null || (licence.Value != member.LicenceType && !licence.Value.IsAny(VstsManager.LicenceTypes.Msdn, VstsManager.LicenceTypes.Advanced, VstsManager.LicenceTypes.Professional)))
                            VstsManager.AssignLicenceToIdentity(identity, member.LicenceType);

                        //Ensure the user is a member of the team
                        if (!members.Any(m => m.UniqueName.EqualsI(member.Email, AppSettings.CurrentUserEmail)))
                        {
                            VstsManager.AddUserToTeam(member.Email, team.Id);
                        }
                    }

                    //Get the new team members
                    var newMembers = model.TeamMembers.Where(m=> !members.Any(m1=>m1.UniqueName.EqualsI(m.Email))).ToList();

                    members = VstsManager.GetMembers(model.Id, team.Id);

                    //Remove old users
                    foreach (var member in members)
                    {
                        if (member.UniqueName.EqualsI(AppSettings.CurrentUserEmail))continue;

                        if (!model.TeamMembers.Any(m => m.Email.EqualsI(member.UniqueName)))
                        {
                            VstsManager.RemoveUserFromTeam(model.Id, team.Id, member.UniqueName);
                            members = VstsManager.GetMembers(model.Id, team.Id);
                        };
                    }

                    //TODO Create the service endpoints into azure if they dont already exist


                    //Send the Email
                    if (newMembers.Count>0)
                    {
                        var projectUrl = project.Links["web"];
                        var appUrl = "https://HelloWorld.com";
                        var gitUrl = repo.RemoteUrl;

                        var notify = new GovNotifyAPI();
                        foreach (var member in newMembers)
                        {
                            var personalisation = new Dictionary<string, dynamic> { { "name", member.Name }, { "email", member.Email }, { "project", model.Name }, { "projecturl", projectUrl }, { "giturl", gitUrl }, { "appurl", appUrl } };
                            notify.SendEmail(member.Email, AppSettings.WelcomeTemplateId, personalisation);
                        }
                    }
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
            if (command=="Delete Project")
            {
                var project = VstsManager.GetProjects().FirstOrDefault(p => p.Id == model.Id);
                if (project == null) return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });
                model.Properties = VstsManager.GetProjectProperties(model.Id, ProjectProperties.CreatedBy);
                if (model.Properties[ProjectProperties.CreatedBy]!=AppSettings.ProjectCreatedBy) return View("CustomError", new CustomErrorViewModel { Title = "Unauthorised", Subtitle = "You dont have permission to delete this project", Description = "This project was not created here and cannot be deleted.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });

                //TODO Delete the project
                VstsManager.DeleteProject(model.Id);
                return View("CustomError", new CustomErrorViewModel { Title = "Complete", Subtitle = "Project successfully deleted", Description = $"Your project was successfully deleted.", CallToAction = "View projects...", ActionText = "Continue", ActionUrl = Url.Action("Index") });
            }
            return Create(model, command);
        }
    }
}
