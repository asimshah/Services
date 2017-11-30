using Fastnet.Core.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Fastnet.Services.Tasks
{
    public static class Extensions
    {
        public static IServiceCollection AddFastnetServiceTasks(this IServiceCollection services, IConfiguration configuration)
        {            
            services.AddScheduler(configuration)
                //.AddSingleton<ScheduledTask, DiagnosticTask>()
                .AddSingleton<ScheduledTask, ConfigureBackups>()
                .AddSingleton<ScheduledTask, BackupService>()
                .AddSingleton<ScheduledTask, PollingService>()
                ;
            ;
            return services;
        }
    }
}
