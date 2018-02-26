using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Fastnet.Core.Web;
using Fastnet.Services.Data;
using Fastnet.Services.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Fastnet.Services.Web
{
    public class Startup
    {
        private ILogger log;
        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            this.log = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc();
            services.AddOptions();
            services.Configure<ServiceOptions>(Configuration.GetSection("ServiceOptions"));
            services.AddWebDbContextFactory(Configuration);
            services.AddDbContext<ServiceDb>();
            services.AddMvc();
            services.AddFastnetServiceTasks(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>();
            if(serverAddresses != null)
            {
                var addresses = string.Join(", ", serverAddresses.Addresses);
                log.LogInformation($"Listening on {addresses}");
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
                log.LogInformation("environment is development");
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                log.LogInformation("environment is not development");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                try
                {
                    var serviceDb = scope.ServiceProvider.GetService<ServiceDb>();
                    ServiceDbInitialiser.Initialise(serviceDb);
                    //serviceDb.Database.EnsureCreated();
                    //serviceDb.Database.Migrate();
                    //log.LogInformation($"ServiceDb existence checked");
                }
                catch (Exception xe)
                {
                    log.LogError(xe, $"Error checking the serviceDb exists");
                }
            }
        }
    }
}
