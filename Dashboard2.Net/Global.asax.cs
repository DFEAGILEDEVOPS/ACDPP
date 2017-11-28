using Autofac;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Dashboard.NetStandard.Core.Classes;
using Dashboard.NetStandard.Core.Interfaces;
using IContainer = Autofac.IContainer;
using Dashboard.Classes;

namespace Dashboard.Net
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IContainer ContainerIOC;
        public static IClassQueue SaveProjectQueue;
        public static IClassQueue DeleteProjectQueue;

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

            //Create Inversion of Control container
            ContainerIOC = BuildContainerIoC();

            SaveProjectQueue = ContainerIOC.ResolveKeyed<IClassQueue>(Queuenames.SaveProject);
            DeleteProjectQueue = ContainerIOC.ResolveKeyed<IClassQueue>(Queuenames.DeleteProject);

        }

        public static IContainer BuildContainerIoC()
        {
            // validate we have a storage connection
            if (string.IsNullOrWhiteSpace(AppSettings.AzureStorageConnectionString))throw new InvalidOperationException("No Azure Storage connection specified. Check the config.");

            var builder = new Autofac.ContainerBuilder();

            builder.Register(g => new AzureClassQueue(AppSettings.AzureStorageConnectionString, Queuenames.SaveProject)).Keyed<IClassQueue>(Queuenames.SaveProject);
            builder.Register(g => new AzureClassQueue(AppSettings.AzureStorageConnectionString, Queuenames.DeleteProject)).Keyed<IClassQueue>(Queuenames.DeleteProject);

            return builder.Build();
        }

    }
}
