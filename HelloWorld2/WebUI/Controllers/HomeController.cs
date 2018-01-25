using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using govukblank.Models;
using DemoSchool.Data;
using System.Data.Common;

namespace govukblank.Controllers
{
    //[Authorize]
    public class HomeController : Controller
    {
        private readonly SchoolContext _schoolContext;

        public HomeController(SchoolContext schoolContext)
        {
            _schoolContext = schoolContext;
        }

        public IActionResult Index()
        {
            ViewBag.ProjectTitle = Program.ProjectTitle;
            return View();
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

        public IActionResult People()
        {
            var conn = new DbConnectionStringBuilder();
            conn.ConnectionString = Program.DefaultConnection;
            ViewBag.Database = conn.ContainsKey("Database") ? conn["Database"] : conn.ContainsKey("Initial Catalog") ? conn["Initial Catalog"] : null;
            ViewBag.People=_schoolContext.People.Select(p=>p.FullName).ToList();

            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
