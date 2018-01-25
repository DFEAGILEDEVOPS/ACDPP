using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using DemoSchool.Data;
using Microsoft.EntityFrameworkCore;
using AzureApi.Client.Net;

namespace govukblank
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddAuthentication(sharedOptions =>
            //{
            //    sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            //})
            //.AddAzureAd(options => Configuration.Bind("AzureAd", options))
            //.AddCookie();

            // Adds a default in-memory implementation of IDistributedCache.
            services.AddDistributedMemoryCache();

            //Adds session
            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromSeconds(Program.SessionTimeoutSeconds);
                options.Cookie.HttpOnly = true;
            });

            Program.ProjectTitle = Configuration["ProjectTitle"];
            if (Program.ProjectTitle == "#{ProjectTitle}#") Program.ProjectTitle = "Demo Project";

            Console.WriteLine($"Program.ProjectTitle={Program.ProjectTitle}");
            Console.WriteLine($"Configuration[VaultUrl]={Configuration["VaultUrl"]}");
            Console.WriteLine($"Configuration[VaultClientId]={Configuration["VaultClientId"]}");
            Console.WriteLine($"Configuration[VaultClientSecret]={Configuration["VaultClientSecret"]}");
            Console.WriteLine($"Configuration[AzureTenantId]={Configuration["AzureTenantId"]}");
            Console.WriteLine($"Environment.GetEnvironmentVariable(VaultUrl)={Environment.GetEnvironmentVariable("VaultUrl")}");
            Console.WriteLine($"Environment.GetEnvironmentVariable(VaultClientId)={Environment.GetEnvironmentVariable("VaultClientId")}");
            Console.WriteLine($"Environment.GetEnvironmentVariable(VaultClientSecret)={Environment.GetEnvironmentVariable("VaultClientSecret")}");
            Console.WriteLine($"Environment.GetEnvironmentVariable(AzureTenantId)={Environment.GetEnvironmentVariable("AzureTenantId")}");
            Console.WriteLine($"Environment.GetEnvironmentVariable(ASPNETCORE_ENVIRONMENT)={Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}");
            Console.WriteLine($"Program.Environment.IsDevelopment()={Program.Environment.IsDevelopment()}");

            if (Program.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
            {
                Program.DefaultConnection = Configuration.GetConnectionString("DefaultConnection");
                Program.DefaultStorage = Configuration.GetConnectionString("DefaultStorage");
                Program.DefaultCache = Configuration.GetConnectionString("DefaultCache");
            }
            else
            {
                Program.KeyVaultClient = new VaultClient(Configuration["VaultUrl"], Configuration["VaultClientId"], Configuration["VaultClientSecret"], Configuration["AzureTenantId"]);
                Program.DefaultConnection = Program.KeyVaultClient.GetSecret("DefaultConnection");
                Program.DefaultStorage = Program.KeyVaultClient.GetSecret("DefaultStorage");
                Program.DefaultCache = Program.KeyVaultClient.GetSecret("DefaultCache");
            }

            //Add the entity framework database model
            services.AddDbContext<SchoolContext>(options => options.UseSqlServer(Program.DefaultConnection,o => o.EnableRetryOnFailure()));
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseSession();
            app.UseStaticFiles();
            app.UseAuthentication();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
