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

namespace Dashboard.Controllers
{
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

                if (newProject.Properties[ProjectProperties.CreatedBy] != AppSettings.ProjectCreatedBy) continue;
                newProject.Name = project.Name;
                newProject.Id = project.Id;
                newProject.CostCode = newProject.Properties[ProjectProperties.CostCode];
                model.Add(newProject);
            }
            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new ProjectViewModel() { Name= "TestProject1", CostCode="123456" };
            model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel() { FirstName="Stephen",LastName="McCabe", Email="stephen.mccabe@cadenceinnova.com" }  };
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
                    if (c == 0)ModelState.AddModelError("", "You must enter at least one team member member");
                    if (!ModelState.IsValid)return View("Project",model);

                    //Check project name doesnt already exist
                    var projects = VstsManager.GetProjects();
                    var project = projects.FirstOrDefault(p => p.Name.ToLower() == model.Name.ToLower());
                    if (project != null && !model.Name.StartsWithI("TestProject"))
                    {
                        ModelState.AddModelError(nameof(model.Name), "A project with this name already exists");
                        return View("Project",model);
                    }

                    //Create Project (if it doesnt already exist)
                    string projectId = project == null ? null : project.Id;
                    if (project == null)
                    {
                        projectId = VstsManager.CreateProject(model.Name, model.Description);
                        projects = VstsManager.GetProjects();
                        project = projects.FirstOrDefault(p => p.Id == projectId);
                    }

                    //Add the special properties
                    var oldProperties = VstsManager.GetProjectProperties(projectId, ProjectProperties.All);
                    var newProperties = new NameValueCollection();
                    if (!oldProperties.ContainsKey(ProjectProperties.CreatedBy)) newProperties[ProjectProperties.CreatedBy] = AppSettings.ProjectCreatedBy;
                    if (!oldProperties.ContainsKey(ProjectProperties.CreatedDate)) newProperties[ProjectProperties.CreatedDate] = DateTime.Now.ToString();
                    if (!oldProperties.ContainsKey(ProjectProperties.CostCode)) newProperties[ProjectProperties.CostCode] = model.CostCode;
                    if (newProperties.Count>0)VstsManager.SetProjectProperties(projectId, newProperties);

                    //Get the teams
                    var teams = VstsManager.GetTeams(projectId);

                    var team = teams[0];

                    //Get the members
                    var members = VstsManager.GetMembers(projectId, team.Id);

                    //Get the Repo
                    var repos = VstsManager.GetRepos(projectId);
                    var repo = repos[0];

                    //copy the sample repo

                    if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
                    {
                        var serviceEndpointId = VstsManager.CreateEndpoint(projectId, AppSettings.SourceRepoUrl, "", AppSettings.VSTSPersonalAccessToken);
                        VstsManager.ImportRepo(projectId, repo.Id, AppSettings.SourceRepoUrl, serviceEndpointId);
                    }

                    //Create users
                    foreach (var member in model.TeamMembers)
                    {
                        VstsManager.AddUserToAccount(VstsManager.Account,member.Email);
                    }

                    //TODO Remove old users


                    //Send the Email
                    if (command == "Create Project")
                    {
                        var notify = new GovNotifyAPI();
                        var projectUrl = $"https://{VstsManager.Account}.visualstudio.com/{WebUtility.UrlEncode(project.Name)}";
                        foreach (var member in model.TeamMembers)
                        {
                            var personalisation = new Dictionary<string, dynamic> { { "name", member.Name }, { "email", member.Email }, { "project", model.Name }, { "projecturl", projectUrl }, { "giturl", repo.RemoteUrl }, { "appurl", "https://HelloWorld.com" } };
                            notify.SendEmail("stephen.mccabe@education.gov.uk", AppSettings.WelcomeTemplateId, personalisation);

                            //var html = Properties.Resources.Welcome;
                            //html = html.ReplaceI("((name))", member.Name).ReplaceI("((email))", member.Email).ReplaceI("((project))", model.TeamProjectName).ReplaceI("((projecturl))", projectUrl).ReplaceI("((appurl))", "https://google.com").ReplaceI("((giturl))", repo.RemoteUrl);
                            //Email.QuickSend("Welcome to the Platform Thing", "stephen.mccabe@cadenceinnova.com", "The Platform Thing", null, $"{member.Name}<{member.Email}>", html, AppSettings.SmtpServer, AppSettings.SmtpUsername, AppSettings.SmtpPassword);
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
            if (teams.Count>0)model.TeamMembers = VstsManager.GetMembers(project.Id,teams[0].Id).Select(m=>new TeamMemberViewModel() { FirstName = m.DisplayName.BeforeFirst(" ").BeforeFirst("."), LastName = m.DisplayName.AfterFirst(" ").AfterFirst("."), Email = m.UniqueName }).ToList();
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
            return Create(model, "Save Changes");
        }
    }
}
