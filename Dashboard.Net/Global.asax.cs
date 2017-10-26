using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Dashboard.Net
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public class TrimModelBinder : DefaultModelBinder
        {
            protected override void SetProperty(ControllerContext controllerContext, ModelBindingContext bindingContext, PropertyDescriptor propertyDescriptor, object value)
            {
                if (propertyDescriptor.PropertyType == typeof(string))
                {
                    var val = value as string;
                    value = string.IsNullOrEmpty(val) ? val : val.Trim();
                }

                base.SetProperty(controllerContext, bindingContext, propertyDescriptor, value);
            }
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            RouteTable.Routes.MapMvcAttributeRoutes();

            ModelBinders.Binders.DefaultBinder = new TrimModelBinder();

            //Remove X-AspNetMvc header for security reasons as per ITHC-- >
            MvcHandler.DisableMvcResponseHeader = true;
        }

    }
}
