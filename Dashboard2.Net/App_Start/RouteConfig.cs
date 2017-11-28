using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace Dashboard.Net
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.IgnoreRoute("Content/{*pathInfo}");
            routes.IgnoreRoute("fonts/{*pathInfo}");
            routes.IgnoreRoute("public/{*pathInfo}");
            routes.IgnoreRoute("scripts/{*pathInfo}");
        }
    }
}
