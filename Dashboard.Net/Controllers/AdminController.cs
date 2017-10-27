using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dashboard.Models;
using Dashboard.Classes;
using Extensions;
using System.Net;
using System.Web.Mvc;
using Dashboard.Net.Models;
using VstsApi.Net;
using Extensions.Net.Classes;

namespace Dashboard.Controllers
{
    [BasicAuthentication()]
    [RoutePrefix("admin")]
    [Route("{action}")]
    public class AdminController : Controller
    {
        [Route]
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }
    }
}
