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
using Extensions.Net.Classes;

namespace Dashboard.Controllers
{
    [BasicAuthentication()]
    [RoutePrefix("Home")]
    [Route("{action}")]
    public class HomeController : Controller
    {
        [Route("~/")]
        public ActionResult Redirect()
        {
            return RedirectToAction("Index", "Projects");
        }

        [Route("~/about")]
        public ActionResult About()
        {

            return View();
        }

        [Route("~/contactus")]
        public ActionResult ContactUs()
        {

            return View();
        }

        [Route("~/feedback")]
        [HttpGet]
        public ActionResult Feedback()
        {
            var model = new FeedbackViewModel();
            return View(model);
        }

        [Route("~/feedback")]
        [HttpPost]
        public ActionResult Feedback(FeedbackViewModel model)
        {

            return View(model);
        }

        [Route("~/error")]
        public ActionResult Error()
        {
            return View(new ErrorViewModel());
        }
    }
}
