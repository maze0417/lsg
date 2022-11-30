using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using LSG.Core;
using LSG.Hosts.LoggerWorker;
using LSG.Hosts.LsgApi;
using LSG.Hosts.LsgFrontend;
using LSG.Infrastructure.DataServices;
using LSG.Infrastructure.DataServices.Data;
using LSG.Infrastructure.HostServices;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LSG.Hosts.GenericHost;

public class Program
{
    private static readonly Dictionary<string, Type> SiteMapper =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { Const.Sites.LoggerWorker, typeof(LoggerWorkerStartup) },
            { Const.Sites.LsgApi, typeof(LsgApiStartup) },
            { Const.Sites.LsgFrontend, typeof(LsgFrontendStartup) }
        };

    public static void Main(string[] args)
    {
        var rootCommand = new RootCommand
        {
            new Option<string>(
                "--site",
                () => throw new InvalidCastException("sites should be specific"),
                "run as input site"),
            new Option<bool>(new[] { "--migrate", "-m" },
                () => false,
                "execute db migration")
        };
        rootCommand.Description = "Lsg site startup";
        rootCommand.Handler = CommandHandler.Create<string, bool>((site, migrate) =>
        {
            var (key, value) = SiteMapper.FirstOrDefault(a => a.Key.IgnoreCaseEquals(site));
            if (key.IsNullOrEmpty()) throw new InvalidCastException($"{site}`s mapping startup is not exist");

            var builder = BaseStartup.CreateHostBuilder(value).Build();
            var serviceProvider = builder.Services;
            var logger = serviceProvider.GetRequiredService<ILsgLogger>();

            if (migrate)
            {
                var factory = serviceProvider.CreateScope().ServiceProvider;


                using var repo = factory.GetRequiredService<ILsgRepository>();
                repo.MigrateToLatestVersion();
                DataSeeder.SeedAsync(factory).GetAwaiter().GetResult();
                return;
            }

            try
            {
                builder.Run();
            }
            catch (Exception e)
            {
                logger.LogError(Const.SourceContext.RunHost, e, $"Failed to run  site : {key}");
                throw;
            }
        });
        rootCommand.Invoke(args);
    }
}