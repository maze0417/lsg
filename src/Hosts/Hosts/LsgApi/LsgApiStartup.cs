using System;
using LSG.Core;
using LSG.Infrastructure;
using LSG.Infrastructure.DataServices;
using LSG.Infrastructure.DataServices.Data;
using LSG.Infrastructure.HostServices;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LSG.Hosts.LsgApi;

public sealed class LsgApiStartup : BaseStartup
{
    private readonly IHostEnvironment _environment;

    public LsgApiStartup(IHostEnvironment environment)
    {
        _environment = environment;
    }

    protected override string Site => Const.Sites.LsgApi;

    protected override void ConfigureCustomServices(IServiceCollection services)
    {
        services.AddSingleton<IServerStatusReporter, LsgApiServerStatusReporter>();

        services.AddSecurity();
        services.AddDataService();
        services.AddServerNotifier();
    }

    protected override void ConfigureAfterEndpoints(IApplicationBuilder app)
    {
        if (_environment.IsDevOrIntOrTest())
        {
            var factory = app.ApplicationServices.CreateScope().ServiceProvider;
            using var repo = factory.GetRequiredService<ILsgRepository>();
            repo.MigrateToLatestVersion();
            DataSeeder.SeedAsync(factory).GetAwaiter().GetResult();
        }
    }

    protected override void CustomEndpointConfigure(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", async context =>
        {
            var reporter = context.RequestServices.GetRequiredService<IServerStatusReporter>();
            var serverStatus = await reporter.GetServerStatusAsync();
            var res = new[] { $"{serverStatus.Site} : {serverStatus.Version}" }.ToJson();
            await context.Response.WriteAsync(res);
        });
    }


    protected override void ConfigureBeforeRouting(IApplicationBuilder app)
    {
    }

    protected override void ConfigureBetweenRoutAndEndpoint(IApplicationBuilder app)
    {
    }

    protected override void OnApplicationStarted(IServiceProvider serviceProvider)
    {
    }

    protected override void OnApplicationStopped(IServiceProvider serviceProvider)
    {
    }
}