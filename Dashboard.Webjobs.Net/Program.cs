using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Autofac;
using System.IO;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Extensions;
using Dashboard.NetStandard.Core;
using Dashboard.NetStandard.Classes;

namespace Dashboard.Webjobs.Net
{
    // To learn more about Microsoft Azure WebJobs SDK, please see https://go.microsoft.com/fwlink/?LinkID=320976
    public class Program
    {
        public static IContainer ContainerIOC;

        // Please set the following connection strings in app.config for this WebJob to run:
        // AzureWebJobsDashboard and AzureWebJobsStorage
        static void Main()
        {
            //Culture is required so UK dates can be parsed correctly
            Thread.CurrentThread.CurrentCulture = new CultureInfo(ConfigurationManager.AppSettings["Culture"].ToStringOr("en-GB"));
            Thread.CurrentThread.CurrentUICulture = Thread.CurrentThread.CurrentCulture;

            var config = new JobHostConfiguration();
            if (config.IsDevelopment)
            {
                config.UseDevelopmentSettings();
                foreach (var key in ConfigurationManager.AppSettings.AllKeys)
                {
                    Console.WriteLine($@"APPSETTING[""{key}""]={ConfigurationManager.AppSettings[key]}");
                }
            }

            config.UseCore();
            config.UseTimers();
            config.Tracing.ConsoleLevel = TraceLevel.Verbose;

            //Use the cloud storage for WebJob storage
            if (!string.IsNullOrWhiteSpace(AppSettings.AzureStorageConnectionString))
            {
                config.DashboardConnectionString = AppSettings.AzureStorageConnectionString;
                config.StorageConnectionString = AppSettings.AzureStorageConnectionString;
            }

            //Create Inversion of Control container
            ContainerIOC = BuildContainerIoC();

            var host = new JobHost(config);
            host.RunAndBlock();
        }

        public static IContainer BuildContainerIoC()
        {
            var builder = new ContainerBuilder();

            builder.Register(g => new GovNotifyAPI(AppSettings.GovNotifyClientRef, AppSettings.GovNotifyApiKey, AppSettings.GovNotifyApiTestKey)).As<IGovNotifyAPI>();

            return builder.Build();
        }
    }
}
