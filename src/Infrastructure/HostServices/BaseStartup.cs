using System;
using System.Text;
using System.Text.Json.Serialization;
using FluentValidation.AspNetCore;
using LSG.Core;
using LSG.Infrastructure.Extensions;
using LSG.Infrastructure.Filters;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using LSG.SharedKernel.Nats;
using LSG.SharedKernel.Redis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LSG.Infrastructure.HostServices;

public abstract class BaseStartup
{
    protected abstract string Site { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddErrorHandler();
        services.AddSingleton(services);
        services.AddSingleton<ILsgConfig>(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var env = provider.GetRequiredService<IHostEnvironment>();
            env.ApplicationName = Const.ApplicationName;
            return new LsgConfig(Site, config, env);
        });
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddHealthChecks();
        services.AddMiddleware();
        services.AddSingleton<IErrorMapper, ErrorMapper>();
        services.AddSingleton<IHttpApiClientLogger, HttpApiClientLogger>();
        services.AddControllers(options =>
            {
                options.Filters.Add(new HandleLsgErrorAttribute());
                options.Filters.Add(new ValidateModelAttribute());
            })
            .AddJsonOptions(c =>
            {
                c.JsonSerializerOptions.PropertyNamingPolicy = null;
                c.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        services.AddFluentValidationAutoValidation();
        services.AddRedis();
        services.AddNats();
        services.AddLsgLog();
        services.AddHttpClientWithHandlers();
        services.AddOperatorClient();
        services.AddMessageEnrich();
        services.AddResponseCreator();
        services.AddEventSubject();

        ConfigureCustomServices(services);
    }

    protected abstract void ConfigureCustomServices(IServiceCollection services);

    protected abstract void ConfigureAfterEndpoints(IApplicationBuilder app);
    protected abstract void CustomEndpointConfigure(IEndpointRouteBuilder endpoints);

    protected abstract void ConfigureBeforeRouting(IApplicationBuilder app);

    protected abstract void ConfigureBetweenRoutAndEndpoint(IApplicationBuilder app);


    public void Configure(IApplicationBuilder app, IHostApplicationLifetime appLifetime)
    {
        app.UseExceptionHandler(errorApp => { errorApp.UseCatchAllExceptionHandler(); });
        ConfigureBeforeRouting(app);

        app.UseRouting();
        app.UseHttpCallLogger();
        app.UseIgnoreRoute();
        app.UseNotFoundHandler();
        ConfigureBetweenRoutAndEndpoint(app);

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = async (context, report) =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(report.ToJson());
                }
            });


            endpoints.MapGet("api/status", async context =>
            {
                var reporter = context.RequestServices.GetRequiredService<IServerStatusReporter>();
                var key = context.Request.Query["key"];
                if (key != Const.StatusKey)
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync("Site is running");
                    return;
                }

                var res = await reporter.GetServerStatusAsync();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(res.ToJson());
            });

            endpoints.MapControllers();
            CustomEndpointConfigure(endpoints);
        });
        ConfigureAfterEndpoints(app);

        appLifetime.ApplicationStarted.Register(() =>
        {
            var lsgConfig = app.ApplicationServices.GetRequiredService<ILsgConfig>();

            var logger = app.ApplicationServices.GetRequiredService<ILsgLogger>();
            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses;

            var sb = new StringBuilder();
            sb.AppendLine($"Running Site: {lsgConfig.CurrentSite}");
            foreach (var address in addresses) sb.AppendLine($"Now listening on: {address}");

            sb.AppendLine($"Hosting environment: {lsgConfig.Environment.EnvironmentName}");
            sb.AppendLine($"Content root path: {lsgConfig.Environment.ContentRootPath}");


            logger.LogInformation(Const.SourceContext.RunHost, sb.ToString());
            OnApplicationStarted(app.ApplicationServices);
        });
        appLifetime.ApplicationStopped.Register(() => { OnApplicationStopped(app.ApplicationServices); });
    }


    protected abstract void OnApplicationStarted(IServiceProvider serviceProvider);

    protected abstract void OnApplicationStopped(IServiceProvider serviceProvider);

    public static IHostBuilder CreateHostBuilder(Type type)
    {
        return Host.CreateDefaultBuilder(null)
            .ConfigureWebHostDefaults(builder =>
            {
                builder
                    .UseKestrel(k => k.AddServerHeader = false)
                    .UseStartup(type);
            })
            .UseDefaultServiceProvider((context, options) =>
            {
                options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                // Validate DI on build
                options.ValidateOnBuild = true;
            });
    }
}