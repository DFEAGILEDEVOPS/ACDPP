using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Dashboard.Models;
using Dashboard.Classes;
using Extensions;
using System.Net;
using Microsoft.AspNetCore.Http.Extensions;

namespace Dashboard.Controllers
{
    public class HomeController : Controller
    {

        [HttpGet]
        public IActionResult Index()
        {
            var model = new ProjectViewModel() { TeamProjectName= "TestProject1", CostCode="123456" };
            model.TeamMembers = new List<ProjectViewModel.TeamMember>() { new ProjectViewModel.TeamMember() { Name="Stephen McCabe", Email="stephen.mccabe@cadenceinnova.com" }  };
            //model.TeamMembers.Add(new ProjectViewModel.TeamMember());
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
                if (command == "Clear")
                {
                    for (int i= model.TeamMembers.Count - 1; i >= 0; i--)
                    {
                        var member = model.TeamMembers[i];
                        if (string.IsNullOrWhiteSpace(member.Name) && string.IsNullOrWhiteSpace(member.Email) && model.TeamMembers.Count>1) model.TeamMembers.RemoveAt(i);
                    }
                }
                else if (command == "Create")
                {
                    int c = 0;
                    foreach (var member in model.TeamMembers)
                    {
                        if (string.IsNullOrWhiteSpace(member.Name) && string.IsNullOrWhiteSpace(member.Email)) continue;
                        c++;
                    }
                    if (c == 0)ModelState.AddModelError("", "You must enter at least one team member member");
                    if (!ModelState.IsValid)return View(model);

                    //Check project name doesnt already exist
                    var projects = VSTSManager.GetProjects();
                    var project = projects.FirstOrDefault(p => p.Name.ToLower() == model.TeamProjectName.ToLower());
                    if (project != null && !model.TeamProjectName.StartsWithI("TestProject"))
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

                    //copy the sample repo

                    if (string.IsNullOrWhiteSpace(repo.DefaultBranch))
                    {
                        var serviceEndpointId = VSTSManager.CreateEndpoint(projectId, AppSettings.SourceRepoUrl, "", AppSettings.VSTSPersonalAccessToken);
                        VSTSManager.ImportRepo(projectId, repo.Id, AppSettings.SourceRepoUrl, serviceEndpointId);
                    }

                    //Send the Email
                    //var notify = new GovNotifyAPI();
                    var projectUrl = $"https://{VSTSManager.Account}.visualstudio.com/{WebUtility.UrlEncode(project.Name)}";
                    foreach (var member in model.TeamMembers)
                    {
                        //var personalisation = new Dictionary<string, dynamic> { {"name",member.Name}, { "email", member.Email }, { "project", model.TeamProjectName }, { "projecturl", project.Url }, { "giturl", repo.RemoteUrl }, { "appurl", "https://HelloWorld1.com" } };
                        //notify.SendEmail("stephen.mccabe@education.gov.uk", AppSettings.WelcomeTemplateId, personalisation);

                        var html = Properties.Resources.Welcome;
                        html = html.ReplaceI("((name))", member.Name).ReplaceI("((email))", member.Email).ReplaceI("((project))", model.TeamProjectName).ReplaceI("((projecturl))", projectUrl).ReplaceI("((appurl))", "https://google.com").ReplaceI("((giturl))", repo.RemoteUrl);
                        Email.QuickSend("Welcome to the Platform Thing", "stephen.mccabe@cadenceinnova.com", "The Platform Thing", null, $"{member.Name}<{member.Email}>", html, AppSettings.SmtpServer, AppSettings.SmtpUsername, AppSettings.SmtpPassword);
                    }
                    return View("CustomError",new CustomErrorViewModel {  Title="Complete", Subtitle="Project successfully created", Description="Your project was successfully created and welcome emails sent to the team members.",  CallToAction="Create another project...", ActionText="Continue", ActionUrl=Request.GetDisplayUrl()});
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
