using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
//using Microsoft.AspNetCore.Hosting;
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
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Fastnet.Core;

namespace Fastnet.Services.Web
{
    //static class __ex
    //{
    //    public static SchedulerService GetSchedulerService(this IServiceProvider sp)
    //    {
    //        return sp.GetService<IHostedService>() as SchedulerService;
    //    }
    //    public static T GetScheduledTask<T>(this IServiceProvider sp) where T : ScheduledTask
    //    {
    //        return sp.GetServices<ScheduledTask>().SingleOrDefault(x => x is T) as T;
    //    }
    //    public static T GetRealtimeTask<T>(this IServiceProvider sp) where T : RealtimeTask
    //    {
    //        return sp.GetServices<RealtimeTask>().SingleOrDefault(x => x is T) as T;
    //    }
    //}
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
            services.AddOptions();
            services.Configure<ServiceOptions>(Configuration.GetSection("ServiceOptions"));
            services.Configure<FileSystemMonitorOptions>(Configuration.GetSection("FileSystemMonitorOptions"));
            services.AddWebDbContext<ServiceDb, ServiceDbContextFactory, ServiceDbOptions>(Configuration, "ServiceDbOptions");
            services.AddMvc();
            services.AddFastnetServiceTasks(Configuration);

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime applicationLifetime)
        {
            var serverAddresses = app.ServerFeatures.Get<IServerAddressesFeature>();
            if (serverAddresses != null)
            {
                var addresses = string.Join(", ", serverAddresses.Addresses);
                log.Information($"Listening on {addresses}");
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
                log.Information("environment is development");
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                log.Information("environment is not development");
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
                }
                catch (Exception xe)
                {
                    log.Error(xe, $"Error checking the serviceDb exists");
                }
            }
            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                try
                {
                    var ss = scope.ServiceProvider.GetSchedulerService();
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        await ss.ExecuteNow<ConfigureBackups>();
                    });
                    //var configureTask = scope.ServiceProvider.GetScheduledTask<ConfigureBackups>();
                    //var rrt = scope.ServiceProvider.GetRealtimeTask<RealTimeReplicationTask>();
                    //configureTask.ExecuteAsync
                }
                catch (Exception xe)
                {
                    log.Error(xe, $"Error checking the serviceDb exists");
                }
            }
        }
    }
}
