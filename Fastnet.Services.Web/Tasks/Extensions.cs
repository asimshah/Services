using Fastnet.Core;
using Fastnet.Core.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Fastnet.Services.Web;
using Microsoft.Extensions.Options;

namespace Fastnet.Services.Tasks
{
    public static class Extensions
    {
        public static IServiceCollection AddFastnetServiceTasks(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<FileSystemMonitorFactory>();
            services.AddScheduler(configuration)
                //.AddSingleton<ScheduledTask, DiagnosticTask>()
                .AddSingleton<RealtimeTask, RealTimeReplicationTask>()
                .AddSingleton<ScheduledTask, ConfigureBackups>()
                .AddSingleton<ScheduledTask, BackupService>()
                .AddSingleton<ScheduledTask, ReplicationService>()
                .AddSingleton<RealtimeTask, PollingService>()
                .AddSingleton<ScheduledTask, InformationService>()
            ;           
            return services;
        }
    }
}
