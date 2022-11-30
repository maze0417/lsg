using System;
using LSG.Core;
using LSG.Infrastructure;
using LSG.Infrastructure.HostServices;
using LSG.SharedKernel.Elk;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Hosts.LoggerWorker
{
    public class LoggerWorkerStartup : BaseStartup
    {
        protected override string Site => Const.Sites.LoggerWorker;

        protected override void ConfigureCustomServices(IServiceCollection services)
        {
            services.AddSingleton<IServerStatusReporter, LoggerWorkerServerStatusReporter>();
            services.AddSingleton<ILogMessageQueueHandler, LogMessageQueueHandler>();
            services.AddElk();
        }

        protected override void OnApplicationStarted(IServiceProvider serviceProvider)
        {
            var listener = serviceProvider.GetRequiredService<ILogMessageQueueHandler>();
            listener.StartReceiveMessages();
        }

        protected override void OnApplicationStopped(IServiceProvider serviceProvider)
        {
            var listener = serviceProvider.GetRequiredService<ILogMessageQueueHandler>();
            listener.StopReceiveMessages();
        }

        protected override void ConfigureAfterEndpoints(IApplicationBuilder app)
        {
        }

        protected override void CustomEndpointConfigure(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/", async context =>
            {
                var reporter = context.RequestServices.GetRequiredService<IServerStatusReporter>();
                var serverStatus = await reporter.GetServerStatusAsync();
                var res = new[] {$"{serverStatus.Site} : {serverStatus.Version}"}.ToJson();
                await context.Response.WriteAsync(res);
            });
        }


        protected override void ConfigureBeforeRouting(IApplicationBuilder app)
        {
        }

        protected override void ConfigureBetweenRoutAndEndpoint(IApplicationBuilder app)
        {
        }
    }
}