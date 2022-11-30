using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages.Hub;
using LSG.Infrastructure;
using LSG.Infrastructure.HostServices;
using LSG.IntegrationTests.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: Timeout(30000)]

namespace LSG.IntegrationTests;

public abstract class Base<T> where T : BaseStartup
{
    private readonly ConcurrentBag<HubConnection> _hubConnections = new();
    protected string CurrentSite;
    protected IServiceProvider DefaultFactory;

    [SetUp]
    public virtual Task Init()
    {
        var hostBuilder = BaseStartup.CreateHostBuilder(typeof(T))
            .ConfigureAppConfiguration((hostingContext, _) =>
            {
                var env = hostingContext.HostingEnvironment;
                if (env.IsProduction())
                {
                    Console.WriteLine(
                        $@"Changing env from {env.EnvironmentName} to {Const.Environments.Development}");
                    env.EnvironmentName = Const.Environments.Development;
                }
            })
            .ConfigureServices(collection =>
            {
                var descriptorToAdd = new ServiceDescriptor(typeof(IHttpApiClientLogger),
                    typeof(TestConsoleClientLogger), ServiceLifetime.Singleton);

                collection.Replace(descriptorToAdd);
                collection.AddTransient<UserAgentHandler>();

                collection.AddHttpClient(Const.Correlation.HttpClient)
                    .AddHttpMessageHandler<UserAgentHandler>();
                collection.AddLsgApiClient();
            });
        DefaultFactory = hostBuilder.Build().Services.CreateScope().ServiceProvider;
        CurrentSite = DefaultFactory.GetRequiredService<ILsgConfig>().CurrentSite;

        return Task.CompletedTask;
    }

    [TearDown]
    public virtual async Task TearDown()
    {
        foreach (var connection in _hubConnections)
            if (connection != null)
                await connection.DisposeAsync();

        _hubConnections.Clear();
    }

    protected async Task<HubConnection> ConnectToChatAsync(string userToken,
        string url,
        TaskCompletionSource<UserExpiredMessage> tsExpired = null,
        TaskCompletionSource<PlayerLoggedMessage> tsPlayerLogged= null)
    {
        var connection = await BuildHubConnectionAsync(
            userToken, url, 
            tsExpired: tsExpired,
            tsOtherPlayerLogged:tsPlayerLogged
            );

        AddToTearDownList(connection);

        return connection;
    }

    protected async Task<HubConnection> ConnectToLiveStreamApiAsync(string userToken,
        string url,
        TaskCompletionSource<UserExpiredMessage> tsExpired = null,
        TaskCompletionSource<PlayerLoggedMessage> tsOtherPlayerLogged = null
    )
    {
        var connection = await BuildHubConnectionAsync(userToken, url,
            tsExpired,
            tsOtherPlayerLogged);

        AddToTearDownList(connection);

        return connection;
    }

    private static async Task<HubConnection> BuildHubConnectionAsync(string userToken, string url,
        TaskCompletionSource<UserExpiredMessage> tsExpired = null,
        TaskCompletionSource<PlayerLoggedMessage> tsOtherPlayerLogged = null
    )
    {
        var connection = BuildHubConnection(url, userToken);

        connection.On<UserExpiredMessage>(Const.Hub.Channels.UserExpired,
            message => { tsExpired?.SetResult(message); });
        connection.On<PlayerLoggedMessage>(Const.Hub.Channels.PlayerLogged,
            message => { tsOtherPlayerLogged?.SetResult(message); });

        await connection.StartAsync();

        return connection;
    }

    private static HubConnection BuildHubConnection(string hubUrl, string userToken)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => { options.AccessTokenProvider = () => Task.FromResult(userToken); })
            .ConfigureLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddConsole();
            })
            .Build();

        connection.Reconnected += s =>
        {
            Console.WriteLine($@"signalr connection Reconnected {s}");
            return Task.CompletedTask;
        };
        connection.Closed += error =>
        {
            Console.WriteLine($@"signalr connection closed {error}");
            return Task.CompletedTask;
        };
        return connection;
    }

    private void AddToTearDownList(HubConnection connection)
    {
        _hubConnections.Add(connection);
    }
}

internal static class BaseExtension
{
    internal static string AppendGroupToHubUrl(this string url, string group)
    {
        return $"{url}?Group={group}";
    }
}