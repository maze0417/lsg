using Builds.Deployment.Contexts;
using Builds.Deployment.Enums;
using Nuke.Common;

namespace Builds.Deployment.Builds;

public partial class DockerBuild
{
    private Target RunIntegrationTests => _ => _
        .OnlyWhenDynamic(() =>
            (_buildContext.Action & ActionType.RunIntegrationTests) == ActionType.RunIntegrationTests)
        .DependsOn(CheckEachSite)
        .Executes(() =>
        {
            const string image = "lsgtest:test";
            _shellTasks.ExecuteScript(
                $"docker build -t {image} .");


            _shellTasks.ExecuteScript(
                $"docker run --rm -e TEAMCITY_VERSION=2020.1 -e ASPNETCORE_ENVIRONMENT={_buildContext.Environment} --network={DockerContext.IntegrationTestDockerNetwork} {image} dotnet test --filter 'TestCategory!=LocalOnly & TestCategory!=Unit & TestCategory!=Wip'");
        });

    private Target CleanIntegrationTestsContainer => _ => _
        .OnlyWhenDynamic(() => _buildContext.Action == ActionType.RunIntegrationTests)
        .DependsOn(RunIntegrationTests)
        .Executes(() =>
        {
            _shellTasks.ExecuteScript(
                @$"docker-compose -p ""{_buildContext.Environment}"" -f ""docker-compose.yml"" -f ""docker-compose.IntegrationTest.yml"" down");
        });

    private Target RunUnitTests => _ => _
        .OnlyWhenDynamic(() => _buildContext.Action == ActionType.RunUnitTests)
        .DependsOn(PrepareShellTask)
        .Executes(() =>
        {
            const string image = "lsgtest:test";
            _shellTasks.ExecuteScript(
                $"docker build -t {image} .");
            _shellTasks.ExecuteScript(
                $"docker run --rm -e TEAMCITY_VERSION=2020.1 -e ASPNETCORE_ENVIRONMENT={_buildContext.Environment} --network={DockerContext.IntegrationTestDockerNetwork} {image} dotnet test -c Release --filter 'TestCategory=Unit & TestCategory!=Wip'");
        });
}