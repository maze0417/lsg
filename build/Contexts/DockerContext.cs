using Builds.Deployment.Extensions;

namespace Builds.Deployment.Contexts;

public sealed class DockerContext : BaseContext
{
    [ShowInLog]
    public string EnvConfigFilePath =>
        $@"{SolutionRootDirectory}/build/ConfigFiles/env.{Environment.Description()}.json";


    [ShowInLog] public static string IntegrationTestDockerNetwork => "tc_net";

  
    public string Agent { get; set; }

    [ShowInLog] public string RegistryUrl => "harbor.soga.club/lsghost";

    [ShowInLog] public string RegistryId => "admin";

    [ShowInLog] public string RegistryPwd => "admin12345";

    public string RemoteImage => $"{RegistryUrl}/lsghost:{Environment.ToString().ToLower()}-{Version}";
    public string LocalImageName => $"lsghost:{Environment.ToString().ToLower()}";


    [ShowInLog]
    public string LocalDockerComposeFilePath => $@"{SolutionRootDirectory}/docker-compose.{Environment}.yml";

  
    [ShowInLog]
    public string RemoteDockerComposeFilePath => $"/home/{DeploymentConfig.BuildUser}/docker-compose.{Environment}.yml";

    public string DockerComposeProject => Environment.ToString().ToLower();
}