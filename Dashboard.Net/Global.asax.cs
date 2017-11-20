using AzureApi.Client.Net;
using Dashboard.Classes;
using Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
            //string serverName= "sqlsrv-t1te-acdpp";
            //string adminUsername = $"{serverName}admin";
            //string adminPassword = Crypto.GeneratePassword();

            //serverName = AzureApi.Net.SqlDatabaseBuilder.CreateSqlServer(serverName, AppSettings.AzureResourceGroup, adminUsername, adminPassword, AppSettings.AppStartIP, AppSettings.AppEndIP);

            //string databaseName = "db-t1te-acdpp";
            //databaseName = AzureApi.Net.SqlDatabaseBuilder.CreateSqlDatabase(AppSettings.AzureResourceGroup, serverName, databaseName);

            //string connectionString = $"Server=tcp:{serverName}.database.windows.net,1433;Initial Catalog={databaseName};Persist Security Info=False;User ID={adminUsername};Password={adminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";

            //var secretKey = $"{databaseName}-ConnectionString";

            //var secretId =VaultClient.SetSecret(secretKey, connectionString);
            //var secret = VaultClient.GetSecret(secretKey);

            //Debugger.Break();


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
