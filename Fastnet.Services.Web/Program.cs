using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Extensions.DependencyInjection;
using System.ServiceProcess;
using Fastnet.Core.Web.Logging;
using Fastnet.Core;

namespace Fastnet.Services.Web
{
    public class Program
    {
        //public static void Main(string[] args)
        //{
        //    BuildWebHost(args).Run();
        //}

        //public static IWebHost BuildWebHost(string[] args) =>
        //    WebHost.CreateDefaultBuilder(args)
        //        .UseStartup<Startup>()
        //        .Build();
        public static void Main(string[] args)
        {
            bool isService = true;
            if (Debugger.IsAttached || args.Contains("--console"))
            {
                isService = false;
            }

            var pathToContentRoot = Directory.GetCurrentDirectory();
            if (isService)
            {
                var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                pathToContentRoot = Path.GetDirectoryName(pathToExe);
            }

            var host = WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(pathToContentRoot)
                .ConfigureLogging(x => x.AddWebRollingFile())
                .UseStartup<Startup>()
                //.UseApplicationInsights()
                .UseUrls("http://localhost:5566")
                .Build();




            //var host = new WebHostBuilder()
            //    .UseKestrel()
            //    .UseContentRoot(pathToContentRoot)
            //    .ConfigureAppConfiguration((bc, config) =>
            //    {
            //        var env = bc.HostingEnvironment;
            //        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            //        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            //    })
            //    .UseIISIntegration()
            //    .ConfigureLogging(x => {
            //        x.AddDebug();
            //        x.AddWebRollingFile();
            //        })
            //    .UseStartup<Startup>()
            //    //.UseApplicationInsights()
            //    .UseUrls("http://localhost:5566")
            //    .Build();

            if (isService)
            {
                //host.RunAsService();
                host.RunAsFastnetService();
            }
            else
            {
                host.Run();
            }
        }


    }
    internal class FastnetService : WebHostService
    {
        private ILogger log;

        public FastnetService(IWebHost host) : base(host)
        {
            log = host.Services.GetRequiredService<ILogger<FastnetService>>();
        }

        protected override void OnStarting(string[] args)
        {
            log.Information("OnStarting() called.");
            base.OnStarting(args);
        }

        protected override void OnStarted()
        {
            log.Information("OnStarted() called.");
            base.OnStarted();
        }

        protected override void OnStopping()
        {
            log.Information("OnStopping() called.");
            base.OnStopping();
        }
    }
    public static class WebHostServiceExtensions
    {
        public static void RunAsFastnetService(this IWebHost host)
        {
            var webHostService = new FastnetService(host);
            ServiceBase.Run(webHostService);
        }
    }
}
