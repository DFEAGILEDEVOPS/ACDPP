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
using Dashboard.NetStandard.Classes;
using Newtonsoft.Json;
using VstsApi.Net.Classes;
using Dashboard.Net;

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
                if (!newProject.Properties.ContainsKey(ProjectProperties.CreatedBy) || newProject.Properties[ProjectProperties.CreatedBy] != AppSettings.ProjectCreatedBy) continue;

                newProject.Id = project.Id;
                newProject.Name = project.Name;
                newProject.Description = project.Description;
                newProject.CostCode = newProject.Properties.ContainsKey(ProjectProperties.CostCode) ? newProject.Properties[ProjectProperties.CostCode]:null;

                var p = VstsManager.GetProject(newProject.Id);
                newProject.Url = p.Links["web"];
                newProject.AppUrl = newProject.Properties.ContainsKey(ProjectProperties.AppUrl) ? newProject.Properties[ProjectProperties.AppUrl] : null;
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
                    //Check there is at least one user
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

                    //Get the source project
                    var sourceProject = allProjects.FirstOrDefault(p => p.Id==model.Id);

                    //Ensure the old project has finished being created
                    if (sourceProject != null && !sourceProject.State.EqualsI("WellFormed")) throw new Exception("Project has not yet completed creation. Please try again later.");

                    //Check the project name is free
                    var targetProject = allProjects.FirstOrDefault(p => p.Name.EqualsI(model.Name));
                    if (targetProject != null && (sourceProject==null || targetProject.Id!=sourceProject.Id))
                    {
                        ModelState.AddModelError(nameof(model.Name), "A project with this name already exists");
                        return View("Project",model);
                    }

                    #endregion

                    //Queue the saved changes
                    sourceProject.Name = model.Name;
                    sourceProject.Members = new List<Member>();
                    foreach (var member in model.TeamMembers)
                    {
                        sourceProject.Members.Add(new Member
                        {
                            Id=member.TeamMemberId,
                            DisplayName=member.Name,
                            EmailAddress=member.Email,
                            Role=member.Role
                        });
                    }
                    MvcApplication.SaveProjectQueue.Enqueue(sourceProject);

                    //Show success confirmation
                    return View("Complete",model);
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
            if (teams.Count>0)model.TeamMembers = VstsManager.GetMembers(project.Id,teams[0].Id).Select(m=>new TeamMemberViewModel() { TeamMemberId=m.Id, FirstName = m.DisplayName.BeforeFirst(" ").BeforeFirst("."), LastName = m.DisplayName.AfterFirst(" ").AfterFirst("."), Email = m.EmailAddress }).ToList();
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
            var sourceProject = VstsManager.GetProjects().FirstOrDefault(p => p.Id == Id);
            if (sourceProject == null) return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to projects list...", ActionText = "Continue", ActionUrl = Url.Action("Edit",new {Id}) });

            //Load the project 
            var model = new ProjectViewModel();
            model.Id = sourceProject.Id;
            model.Name = sourceProject.Name;
            model.Description = sourceProject.Description;
            var properties = VstsManager.GetProjectProperties(sourceProject.Id, ProjectProperties.CreatedBy, ProjectProperties.CreatedDate, ProjectProperties.CostCode);
            model.CostCode = properties[ProjectProperties.CostCode];

            var teams = VstsManager.GetTeams(sourceProject.Id);
            if (teams.Count > 0) model.TeamMembers = VstsManager.GetMembers(sourceProject.Id, teams[0].Id).Select(m => new TeamMemberViewModel() { TeamMemberId = m.Id, FirstName = m.DisplayName.BeforeFirst(" ").BeforeFirst("."), LastName = m.DisplayName.AfterFirst(" ").AfterFirst("."), Email = m.EmailAddress }).ToList();
            if (model.TeamMembers.Count < 1) model.TeamMembers = new List<TeamMemberViewModel>() { new TeamMemberViewModel() };

            return View("Delete", model);
        }

        [HttpPost]
        public ActionResult Delete(ProjectViewModel model, string projectName)
        {
            ModelState.Exclude(nameof(model.Name),nameof(model.CostCode));

            var sourceProject = VstsManager.GetProjects().FirstOrDefault(p => p.Id == model.Id);
            if (string.IsNullOrWhiteSpace(projectName) || !model.Name.EqualsI(projectName))
            {
                ModelState.AddModelError(nameof(model.Name), "You must enter the exact project name");
                return View(model);
            }

            if (sourceProject == null) return View("CustomError", new CustomErrorViewModel { Title = "Not Found", Subtitle = "Cannot find the specified project", Description = "Your project was not found.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });
            model.Properties = VstsManager.GetProjectProperties(model.Id, ProjectProperties.CreatedBy);
            if (model.Properties[ProjectProperties.CreatedBy] != AppSettings.ProjectCreatedBy) return View("CustomError", new CustomErrorViewModel { Title = "Unauthorised", Subtitle = "You dont have permission to delete this project", Description = "This project was not created here and cannot be deleted.", CallToAction = "Return to editing this project", ActionText = "Continue", ActionUrl = Url.Action("Edit", new { model.Id }) });

            if (sourceProject != null && !sourceProject.State.EqualsI("WellFormed")) throw new Exception("Project is not ready for deletion. Please try again later.");

            //Queue project fro deletion
            MvcApplication.DeleteProjectQueue.Enqueue(sourceProject);

            return View("CustomError", new CustomErrorViewModel { Title = "Complete", Subtitle = "Delete Project", Description = $"Your project has been queued for deletion.", CallToAction = "View projects...", ActionText = "Continue", ActionUrl = Url.Action("Index") });
        }
    }
}
