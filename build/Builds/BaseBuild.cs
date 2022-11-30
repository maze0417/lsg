using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Builds.Deployment.Contexts;
using Builds.Deployment.Enums;
using Builds.Deployment.Extensions;
using Builds.Deployment.Services;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace Builds.Deployment.Builds;

public abstract class BaseBuild : NukeBuild
{
    protected readonly BaseContext ContextBase;

    protected BaseBuild(BaseContext context)
    {
        ContextBase = context;
        Console.OutputEncoding = Encoding.UTF8;
    }


    [Parameter("Agent", Name = "agent")] public string Agent { get; set; }

    [Parameter("Environment", Name = "environment")]
    public string Environment { get; set; }

    [Parameter("Component", Name = "component")]
    public string Component { get; set; }

    [Parameter("User Password", Name = "userPwd")]
    public string UserPassword { get; set; }

    [Parameter("Version", Name = "version")]
    public string Version { get; set; }

    [Parameter("Action", Name = "action")] public string Action { get; set; }

    [Parameter("force point", Name = "force")]
    public bool IsForce { get; set; }

    internal abstract Target Initialize { get; }

    internal abstract Target Finish { get; }

    internal abstract Target PrintParameter { get; }
    protected abstract DeploymentConfig ParseDeploymentConfig();


    protected void SetupEnvironment()
    {
        var (valid, environment) = Environment.Parse<EnvironmentType>();

        if (!valid) throw new InvalidOperationException($"Unsupported environment: {Environment}.");

        ContextBase.Environment = environment;
    }

    protected void SetupComponents()
    {
        ContextBase.Components = Component?
            .Split(new[] { '+' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(component => component.Trim()).ToArray() ?? new string[] { };
    }


    protected void SetupBase()
    {
        ContextBase.SourceDirectory = RootDirectory / "src";
        ContextBase.SolutionSlnFilePath = RootDirectory / "Lsg.sln";
        ContextBase.AssemblyFilePath = ContextBase.SourceDirectory / "GlobalAssemblyInfo.cs";
        ContextBase.SolutionRootDirectory = RootDirectory;
        ContextBase.BuildType = IsLocalBuild ? Configuration.Debug : Configuration.Release;
        ContextBase.Version =  GetType().Assembly.GetName().Version?.ToString();
        ContextBase.IsLocalBuild = IsLocalBuild;
    }

    protected void SetupDeploymentConfig()
    {
        ContextBase.DeploymentConfig = ParseDeploymentConfig();
        ContextBase.DeploymentConfig.Containers = ContextBase.DeploymentConfig.Containers
            .Where(site => !ContextBase.Components.Any() || ContextBase.Components.Contains(site.Component))
            .ToArray();
    }

    protected void SetupAction()
    {
        var (valid, actionType) = Action.Parse<ActionType>();

        if (!valid) throw new InvalidOperationException($"Unsupported actiontype: {Action}.");

        ContextBase.Action = actionType;
    }

    protected void BasePrintParameter()
    {
        var param = GetType()
            .GetProperties()
            .Where(x => x.GetCustomAttributes<ParameterAttribute>().Any()).ToList();
        using (Logger.Block("CLI Params"))
        {
            param.ForEach(a => Logger.Info($"{a.Name} : {a.GetValue(this)}"));
        }

        var context = ContextBase.GetType()
            .GetProperties()
            .Where(x => x.GetCustomAttributes<ShowInLogAttribute>().Any()).ToList();
        using (Logger.Block("Build Context"))
        {
            context.ForEach(a =>
            {
                if (a.PropertyType.Name.Equals(typeof(string[]).Name))
                {
                    var array = (string[])a.GetValue(ContextBase);
                    Logger.Info($"{a.Name} : {array.JoinComma()}");
                    return;
                }

                Logger.Info($"{a.Name} : {a.GetValue(ContextBase)}");
            });
        }
    }
}