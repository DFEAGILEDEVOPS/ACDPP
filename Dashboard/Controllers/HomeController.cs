using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;
using Dashboard.Classes;

namespace Dashboard.Controllers
{
    public class HomeController : Controller
    {

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ProjectViewModel() { TeamProjectName= "TestProject1" };
            model.TeamMembers = new List<ProjectViewModel.TeamMember>();
            model.TeamMembers.Add(new ProjectViewModel.TeamMember());
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(ProjectViewModel model,string command)
        {
            try
            {
                if (command == "Add")
                {
                    model.TeamMembers.Add(new ProjectViewModel.TeamMember());
                }
                else if (command == "Create")
                {
                    //Check project name doesnt already exist
                    var projects = VSTSManager.GetProjects();
                    var project = projects.FirstOrDefault(p => p.Name.ToLower() == model.TeamProjectName.ToLower());
                    if (project != null && model.TeamProjectName!="TestProject1")
                    {
                        ModelState.AddModelError(nameof(model.TeamProjectName), "A project with this name already exists");
                        return View(model);
                    }

                    //Create Project (if it doesnt already exist)
                    string projectId = project == null ? null : project.Id;
                    if (project==null)projectId= VSTSManager.CreateProject(model.TeamProjectName);

                    //Get the teams
                    var teams = VSTSManager.GetTeams(projectId);

                    var team = teams[0];

                    //Get the members
                    var members = VSTSManager.GetMembers(projectId, team.Id);

                    //Create users

                    //Get the Repo
                    var repos = VSTSManager.GetRepos(projectId);
                    var repo = repos[0];

                    var serviceEndpointId=VSTSManager.CreateEndpoint(projectId,AppSettings.SourceRepoUrl,"",AppSettings.VSTSPersonalAccessToken);
                    VSTSManager.ImportRepo(projectId,repo.Id,AppSettings.SourceRepoUrl,serviceEndpointId);

                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            return View(model);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
