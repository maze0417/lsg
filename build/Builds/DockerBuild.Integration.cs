using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Builds.Deployment.Contexts;
using Builds.Deployment.Enums;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace Builds.Deployment.Builds;

public partial class DockerBuild
{
    private Target CleanContainer => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.RunIntegrationTests) == ActionType.RunIntegrationTests)
        .DependsOn(PrepareShellTask)
        .Executes(() =>
        {
            ForEachServer((_, client) =>
            {
                client.ExecuteRemoteScript(
                    @"docker system prune -f");
                client.ExecuteRemoteScript(
                    @$"docker-compose -p ""{_buildContext.DockerComposeProject}"" -f ""docker-compose.{_buildContext.Environment}.yml"" down");
            });
        });

    private Target RunDbContainer => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.ExecuteDbMigration) == ActionType.ExecuteDbMigration)
        .DependsOn(CleanContainer)
        .Executes(() =>
        {
            ForEachServer((ip, client) =>
            {
                client.CopyFileTo(_buildContext.LocalDockerComposeFilePath, ip,
                    _buildContext.RemoteDockerComposeFilePath);
                client.ExecuteRemoteScript(
                    @$"docker-compose -p ""{_buildContext.DockerComposeProject}"" -f ""docker-compose.{_buildContext.Environment}.yml"" up -d mssql"
                );
            });
        });

    private Target BuildImage => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.BuildImage) == ActionType.BuildImage)
        .OnlyWhenDynamic(() => !_buildContext.IsForce)
        .DependsOn(RunDbContainer)
        .Executes(() =>
        {
            var image = $"lsghost:{_buildContext.Environment.ToString().ToLower()}";

            if (_buildContext.Environment == EnvironmentType.IntegrationTest) image = "lsghost:test";


            _shellTasks.ExecuteScript(
                $"docker image rm {image}");

            _shellTasks.ExecuteScript(
                @$"docker-compose -p ""{_buildContext.DockerComposeProject}"" -f ""docker-compose.yml"" -f ""docker-compose.{_buildContext.Environment}.yml"" build lsgapi");
        });

    private Target PushImage => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.PushImage) == ActionType.PushImage)
        .DependsOn(BuildImage)
        .Executes(() =>
        {
            _shellTasks.ExecuteScript(
                @$"docker login -u '{_buildContext.RegistryId}' -p '{_buildContext.RegistryPwd}' {_buildContext.RegistryUrl}");

            var image = $"lsghost:{_buildContext.Environment.ToString().ToLower()}";

            _shellTasks.ExecuteScript(
                $"docker tag {image} {_buildContext.RemoteImage}");

            _shellTasks.ExecuteScript($"docker push {_buildContext.RemoteImage}");

            _shellTasks.ExecuteScript($"docker image rm {_buildContext.RemoteImage}");
        });

    private Target PullImage => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.PullIImage) == ActionType.PullIImage)
        .DependsOn(PushImage)
        .Executes(() =>
        {
            ForEachServer((_, client) =>
            {
                client.ExecuteRemoteScript(
                    @$"docker login -u '{_buildContext.RegistryId}' -p '{_buildContext.RegistryPwd}' {_buildContext.RegistryUrl}"
                );

                client.ExecuteRemoteScript($"docker pull {_buildContext.RemoteImage}");


                client.ExecuteRemoteScript(
                    $"docker tag {_buildContext.RemoteImage} {_buildContext.LocalImageName}");


                client.ExecuteRemoteScript($"docker image rm {_buildContext.RemoteImage}");
            });
        });

    private Target ExecuteDbMigration => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.ExecuteDbMigration) == ActionType.ExecuteDbMigration)
        .DependsOn(PullImage)
        .Executes(() =>
        {
            var image = string.Empty;
            var network = string.Empty;
            if (_buildContext.Environment == EnvironmentType.Integration)
            {
                network = "lsg_integration";
                image = _buildContext.LocalImageName;
            }

            if (_buildContext.Environment == EnvironmentType.IntegrationTest)
            {
                network = DockerContext.IntegrationTestDockerNetwork;
                image = "lsghost:test";
            }

            ForEachServer((_, client) =>
            {
                client.ExecuteRemoteScript(
                    @$"docker run --rm -e ASPNETCORE_ENVIRONMENT={_buildContext.Environment} --network={network} {image} dotnet LSG.Hosts.GenericHost.dll --site lsgapi -m");
            });
        });


    private Target RunContainer => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.RunContainer) == ActionType.RunContainer)
        .DependsOn(ExecuteDbMigration)
        .Executes(() =>
        {
            var targetContainer = _buildContext.Components.JoinSpace();
            ForEachServer((_, client) =>
            {
                client.ExecuteRemoteScript(
                    @$"docker-compose -p ""{_buildContext.DockerComposeProject}"" -f ""docker-compose.{_buildContext.Environment}.yml"" rm -s -f {targetContainer}");

                client.ExecuteRemoteScript(
                    @$"docker-compose -p ""{_buildContext.DockerComposeProject}"" -f ""docker-compose.{_buildContext.Environment}.yml"" up -d {targetContainer}");
            });
        });

    private Target CheckEachSite => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.CheckSite) == ActionType.CheckSite)
        .DependsOn(RunContainer, CleanContainer)
        .Executes(async () =>
        {
            var checkUrls =
                (from domain in _buildContext.DeploymentConfig.Containers
                    select new CheckSite
                    {
                        Name = domain.Component,
                        Method = HttpMethod.Get,
                        Url =
                            _buildContext.Environment == EnvironmentType.IntegrationTest
                                ? $"http://{domain.Component}/api/status?key=4e5461aa-5e68-411c-8395-fecb65460825"
                                : $"http://{domain.Ip}:{domain.ExposePort}/api/status?key=4e5461aa-5e68-411c-8395-fecb65460825",
                        IsOk = false
                    }).ToArray();


            var httpClient = new HttpClient();

            var retryCount = 1;


            do
            {
                var tasks = checkUrls.Where(a => !a.IsOk)
                    .Select(a => RequestSiteAsync(a, httpClient, retryCount)).ToArray();
                using (Logger.Block($"{retryCount}-Request"))
                {
                    await Task.WhenAll(tasks);
                }

                if (checkUrls.All(a => a.IsOk))
                {
                    Logger.Info("check sites ok");
                    break;
                }

                if (retryCount == BaseContext.CheckSiteMaxRetry) throw new Exception("check sites failed");

                await Task.Delay(TimeSpan.FromSeconds(30));
            } while (++retryCount <= BaseContext.CheckSiteMaxRetry);

            async Task RequestSiteAsync(CheckSite context, HttpClient client, int attempt)
            {
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                var cancelToken = new CancellationTokenSource(TimeSpan.FromSeconds(120));
                try
                {
                    var res = await client.SendAsync(new HttpRequestMessage
                    {
                        Content = context.Content?.Invoke(),
                        Method = context.Method,
                        RequestUri = new Uri(context.Url)
                    }, cancelToken.Token);
                    var response = await res.Content.ReadAsStringAsync();
                    stopwatch.Stop();


                    if (!res.IsSuccessStatusCode)
                    {
                        var log = BaseContext.CheckSiteMaxRetry == attempt
                            ? $"Site:{context.Url},statuscode:{res.StatusCode},response:{response} Elapsed:{stopwatch.ElapsedMilliseconds} ms"
                            : $"Site:{context.Url},statuscode:{res.StatusCode}";
                        Logger.Warn(log);
                        context.IsOk = false;
                        return;
                    }

                    Console.WriteLine(
                        $@"Site:{context.Url},OK Elapsed:{stopwatch.ElapsedMilliseconds} ms, response :{response}");
                    context.IsOk = true;
                    return;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var log = BaseContext.CheckSiteMaxRetry == attempt
                        ? $"Site:{context.Url},error detail :{ex} Elapsed:{stopwatch.ElapsedMilliseconds} ms"
                        : $"Site:{context.Url},error :{ex.Message}";
                    Logger.Warn(log);
                }
                finally
                {
                    cancelToken.Dispose();
                }

                context.IsOk = false;
            }
        });
}