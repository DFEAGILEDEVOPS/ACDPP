using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using DemoSchool.Data;
using Microsoft.Extensions.DependencyInjection;
using AzureApi.Client.Net;

namespace govukblank
{
    public class Program
    {
        public static string DefaultConnection = null;
        public static string DefaultStorage = null;
        public static string DefaultCache = null;

        public static int SessionTimeoutSeconds = 20;
        public static VaultClient KeyVaultClient;
        public static IHostingEnvironment Environment;

        public static string ProjectTitle { get; set; } = "Demo Project";

        public static void Main(string[] args)
        {
            var host = BuildWebHost(args);
            
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<SchoolContext>();
                    DbInitializer.Initialize(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while seeding the database.");
                }
            }

            host.Run();

        }

        public static IWebHost BuildWebHost(string[] args) =>

            WebHost.CreateDefaultBuilder(args)
                //.UseKestrel(options =>
                //{
                //    options.Listen(IPAddress.Loopback, 5000);
                //    options.Listen(IPAddress.Loopback, 5001, listenOptions =>
                //    {
                //        listenOptions.UseHttps("HelloWorld1.pfx", "HelloWorld1");
                //    });
                //})
                .ConfigureAppConfiguration((builderContext, config) =>
                {
                    Environment = builderContext.HostingEnvironment;
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddJsonFile($"appsettings.{Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                        .AddUserSecrets<Startup>()
                        .AddEnvironmentVariables();
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>()
                .Build();
    }
}
