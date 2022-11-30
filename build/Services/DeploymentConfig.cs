namespace Builds.Deployment.Services;

public class DeploymentConfig
{
    public string BuildUser { get; set; }
    public string BuildUserPassword { get; set; }
    public string DbPassword { get; set; }

    public Container[] Containers { get; set; }
}

public class Container
{
    public string Component { get; set; }

    public uint ExposePort { get; set; }

    public string Server { get; set; }
    public string Ip { get; set; }
}