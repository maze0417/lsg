using System;
using System.Linq;
using System.Net.Http;
using LSG.Core;
using LSG.Core.Interfaces;
using LSG.Infrastructure.Clients;
using LSG.Infrastructure.DataServices;
using LSG.Infrastructure.Handlers;
using LSG.Infrastructure.Middleware;
using LSG.Infrastructure.Security;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Serilog.Formatting.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;
using ILogger = Serilog.ILogger;

namespace LSG.Infrastructure.HostServices;

public static class BaseStartupExtensions
{
    public static void AddSecurity(this IServiceCollection services)
    {
        services.AddSingleton<IDataEncoder, Base62DataEncoder>();
        services.AddSingleton<ICryptoProvider, CryptoProvider>();
        services.AddSingleton<ITokenProvider, TokenProvider>();
        services.AddSingleton<ITokenExpiration, TokenExpiration>();
    }


    public static void AddLsgLog(this IServiceCollection services)
    {
        services.AddSingleton<ILsgLogger, LsgLogger>();
        services.AddSingleton<IClientIpAnalyzer, ClientIpAnalyzer>();
        services.AddSingleton<Action<LogEvent>>(_ => { });
        services.AddSingleton<ILogger>(provider =>
        {
            var config = provider.GetRequiredService<ILsgConfig>();

            var conf = provider.GetRequiredService<IConfiguration>();


            var configuration = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Environment", config.Environment.EnvironmentName)
                .Enrich.WithProperty("Server", Const.ServerName)
                .Enrich.WithProperty("Site", config.CurrentSite)
                .ReadFrom.Configuration(conf);

            if (config.Environment.IsDevOrIntOrTest())
                configuration
                    .WriteTo.Console(
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{SourceContext} {Level:u3}]-[{CorrelationId}] {Message:lj}{NewLine}{Exception}",
                        theme: SystemConsoleTheme.Literate)
                    .WriteTo.Debug(
                        outputTemplate:
                        "{Timestamp:HH:mm:ss} [{SourceContext} {Level:u3}]-[{CorrelationId}] {Message:lj}{NewLine}{Exception}");


            configuration.WriteTo.MessageQueue(provider, new ElasticsearchJsonFormatter());


            return configuration.CreateLogger();
        });
        services.AddSingleton<ILoggerFactory>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger>();
            return new SerilogLoggerFactory(logger);
        });
    }


    public static void AddMiddleware(this IServiceCollection services)
    {
        typeof(HttpCallLoggerMiddleware).Assembly.ExportedTypes
            .Where(t => typeof(IMiddleware).IsAssignableFrom(t))
            .Where(t => t.IsPublic && !t.IsAbstract).ForEach(m => { services.AddScoped(m); });
    }

    public static void AddErrorHandler(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressConsumesConstraintForFormFileParameters = true;
            options.SuppressModelStateInvalidFilter = true;
        });
        services.AddSingleton<IHttpApiErrorHandler, HttpApiErrorHandler>();
        services.AddSingleton<IClientErrorFactory, ClientErrorFactory>();
    }

    public static void AddHttpClientWithHandlers(this IServiceCollection services)
    {
        typeof(CorrelationIdHandler).Assembly.ExportedTypes
            .Where(t => typeof(DelegatingHandler).IsAssignableFrom(t))
            .Where(t => t.IsPublic && !t.IsAbstract).ForEach(m => { services.AddTransient(m); });

        services.AddHttpClient();
        services.AddHttpClient(Const.Correlation.HttpClient)
            .AddHttpMessageHandler<CorrelationIdHandler>()
            .AddHttpMessageHandler<ForwardHeadersHandler>();
    }

    public static void AddNotifier(this IServiceCollection services)
    {
        services.AddSingleton<ServerNotifier>();
        services.AddSingleton<Func<Type, INotifier>>(provider =>
            type => (INotifier)provider.GetRequiredService(type));
        services.AddSingleton<INotifier, ServerNotifier>();
    }


    public static void AddOperatorClient(this IServiceCollection services)
    {
        services.AddSingleton<IUserApiClient, UserApiClient>();
        services.AddSingleton<IBrandApiClient, BrandApiClient>();
    }

    public static void AddMessageEnrich(this IServiceCollection services)
    {
        services.AddSingleton<IMessageEnrich, MessageEnrich>();
    }

    public static void AddResponseCreator(this IServiceCollection services)
    {
        services.AddSingleton<IResponseCreator, ResponseCreator>();
    }


    public static void AddLsgApiClient(this IServiceCollection services)
    {
        services.AddSingleton<ILsgFrontendClient, LsgFrontendClient>();
    }


    public static void AddServerNotifier(this IServiceCollection services)
    {
        services.AddSingleton<INotifier, ServerNotifier>();
        services.AddSingleton<Func<Type, INotifier>>(provider =>
            _ => provider.GetRequiredService<INotifier>());
    }


    public static void AddDataService(this IServiceCollection services)
    {
        services.AddSingleton<ITransactionManager, TransactionManager>();

        services.AddDbContext<ILsgRepository, LsgRepository>(
            (provider, builder) => ConfigureDbContextOptionsBuilder(provider, builder, false));
        services.AddDbContext<ILsgReadOnlyRepository, LsgReadOnlyRepository>((provider, builder) =>
            ConfigureDbContextOptionsBuilder(provider, builder, true));
        services.AddSingleton<Func<ILsgRepository>>(provider =>
            () => provider.CreateScope().ServiceProvider.GetRequiredService<ILsgRepository>());
        services.AddSingleton<Func<ILsgReadOnlyRepository>>(provider =>
            () => provider.CreateScope().ServiceProvider.GetRequiredService<ILsgReadOnlyRepository>());

        static void ConfigureDbContextOptionsBuilder(IServiceProvider provider, DbContextOptionsBuilder builder,
            bool isReadonly)
        {
            var config = provider.GetRequiredService<ILsgConfig>();

            string connectionString;
            try
            {
                connectionString =
                    isReadonly
                        ? config.LsgReadOnlyConnectionString
                        : config.LsgConnectionString;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException($"Decrypt connection string failed :{e.Message}");
            }


            builder
                .EnableSensitiveDataLogging(config.Environment.IsDevelopment())
                .UseSqlServer(connectionString);
        }
    }


    public static void AddEventSubject(this IServiceCollection services)
    {
        services.AddSingleton(typeof(IInternalEventSubject<>), typeof(InternalEventSubject<>));
    }
}