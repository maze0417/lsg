using System;
using System.Text.Json.Serialization;
using LSG.Core;
using LSG.Infrastructure;
using LSG.Infrastructure.Extensions;
using LSG.Infrastructure.Filters;
using LSG.Infrastructure.Handlers;
using LSG.Infrastructure.HostServices;
using LSG.Infrastructure.HostServices.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace LSG.Hosts.LsgFrontend;

public sealed class LsgFrontendStartup : BaseStartup
{
    private readonly IHostEnvironment _environment;

    public LsgFrontendStartup(IHostEnvironment environment)
    {
        _environment = environment;
    }

    protected override string Site => Const.Sites.LsgFrontend;

    protected override void ConfigureCustomServices(IServiceCollection services)
    {
        services.AddSecurity();
        services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNamingPolicy = null;
                options.PayloadSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        services.AddSpaStaticFiles(configuration => { configuration.RootPath = "dist/GameLobbyApp"; });


        if (_environment.IsDevelopment())
        {
            services.AddCors(options =>
            {
                options.AddPolicy(_environment.EnvironmentName,
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            ;
                    });
            });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LSG API", Version = "v1" });
                c.DocumentFilter<OnlyShowDocFilter>(Const.Sites.LsgFrontend);
            });
        }

        services.AddAuthentication(Const.AuthenticationSchemes.UserToken)
            .AddScheme<AuthenticationSchemeOptions, UserTokenAuthenticationHandler>(
                Const.AuthenticationSchemes.UserToken,
                null);

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Const.AuthenticationSchemes.UserToken,
                policy =>
                {
                    policy.AddAuthenticationSchemes(Const.AuthenticationSchemes.UserToken);
                    policy.RequireAuthenticatedUser();
                });
        });

        services.AddSingleton<IServerStatusReporter, FrontendStatusReporter>();
        services.AddLsgApiClient();
        services.AddNotifier();
        services.AddDataService();
    }

    protected override void ConfigureAfterEndpoints(IApplicationBuilder app)
    {
        app.UseStaticFiles();

        if (!_environment.IsDevelopment()) app.UseSpaStaticFiles();

        if (_environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.RoutePrefix = "doc";
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "LSG API V1");
            });
        }
    }

    protected override void CustomEndpointConfigure(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<ApiHub>(Const.Hub.ApiHubPath);
    }


    protected override void ConfigureBeforeRouting(IApplicationBuilder app)
    {
        if (_environment.IsProduction()) return;

        app.UseRewriter(new RewriteOptions()
            .AddRewrite(@"^demo$", "demo.html", true));
    }

    protected override void ConfigureBetweenRoutAndEndpoint(IApplicationBuilder app)
    {
        if (_environment.IsDevelopment()) app.UseCors(_environment.EnvironmentName);

        app.UseWebSocketAuthentication();
        app.UseAuthorization();
    }

    protected override void OnApplicationStarted(IServiceProvider serviceProvider)
    {
    }

    protected override void OnApplicationStopped(IServiceProvider serviceProvider)
    {
    }
}