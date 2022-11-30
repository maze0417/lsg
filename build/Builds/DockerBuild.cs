using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text.Json;
using Builds.Deployment.Contexts;
using Builds.Deployment.Enums;
using Builds.Deployment.Extensions;
using Builds.Deployment.Services;
using Builds.Deployment.Tasks;
using Nuke.Common;

namespace Builds.Deployment.Builds;

public sealed partial class DockerBuild : BaseBuild
{
    private static readonly ConcurrentDictionary<string, IShellTask> SshClientsByHost = new();
    private readonly DockerContext _buildContext;
    private IShellTask _shellTasks;


    public DockerBuild() : base(new DockerContext())
    {
        _buildContext = (DockerContext)ContextBase;
    }

    internal override Target Initialize => _ => _
        .Requires(() => Action.ShouldNotNullOrEmpty())
        .Requires(() => Environment.ShouldNotNullOrEmpty())
        .Executes(() =>
        {
            SetupEnvironment();
            SetupComponents();
            SetupBase();
            SetupDeploymentConfig();
            SetupAction();
            _buildContext.IsForce = IsForce;
            var pwd = EncryptProvider.Decrypt(_buildContext.DeploymentConfig.BuildUserPassword);
            _buildContext.ClearPwd = pwd;
            _buildContext.Agent = Agent;
        });

    internal override Target PrintParameter => _ => _
        .DependsOn(Initialize)
        .Executes(BasePrintParameter);


    private Target CheckVersionAndPasswordIfNotDev => _ => _
        .OnlyWhenDynamic(() =>
            _buildContext.Environment ==
            EnvironmentType.Production) // don`t check this because deployment is on mickey site
        .DependsOn(PrintParameter)
        .Executes(() =>
        {
            ThrowIfVersionMismatch();

            void ThrowIfVersionMismatch()
            {
                if (_buildContext.Version != Version)
                    throw new InvalidOperationException(
                        $"The version given is {Version} but the existing one in the code is {_buildContext.Version}.");
            }
        });

    private Target PrepareShellTask => _ => _
        .DependsOn(CheckVersionAndPasswordIfNotDev)
        .Executes(() =>
        {
            _shellTasks = new SshShellTasks(_buildContext.DeploymentConfig.BuildUser, _buildContext.ClearPwd,
                _buildContext.SolutionRootDirectory);

            var servers = from domain in _buildContext.DeploymentConfig.Containers
                group domain by domain.Ip;


            foreach (var server in servers)
            {
                SshClientsByHost.AddOrUpdate(server.Key, _shellTasks.EnterSession(server.Key),
                    (_, client) => client);
            }
               
        });

    internal override Target Finish => _ => _
        .DependsOn(
            RunUnitTests,
            CleanIntegrationTestsContainer,
            CheckEachSite)
        .Executes(() => { });

    protected override DeploymentConfig ParseDeploymentConfig()
    {
        return JsonSerializer.Deserialize<DeploymentConfig>(
            File.ReadAllText(_buildContext.EnvConfigFilePath));
    }

    private void ForEachServer(Action<string, IShellTask> action)
    {
        var servers = from domain in _buildContext.DeploymentConfig.Containers
            group domain by domain.Ip;


        foreach (var server in servers)
        {
            var ip = server.Key;
            if (!SshClientsByHost.TryGetValue(ip, out var client))
                throw new Exception(
                    $"Can't get client for {ip}, please check Target {nameof(PrepareShellTask)}");

            action(server.Key, client);
        }
    }
}