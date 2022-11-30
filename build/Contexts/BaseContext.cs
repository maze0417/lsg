using System;
using System.Net.Http;
using Builds.Deployment.Enums;
using Builds.Deployment.Services;
using Nuke.Common.IO;

namespace Builds.Deployment.Contexts
{
    public abstract class BaseContext
    {
        [ShowInLog] public AbsolutePath SourceDirectory { get; set; }

        [ShowInLog] public AbsolutePath SolutionRootDirectory { get; set; }

        [ShowInLog] public AbsolutePath SolutionSlnFilePath { get; set; }

        [ShowInLog] public AbsolutePath AssemblyFilePath { get; set; }


        [ShowInLog] public string BuildType { get; set; }

        [ShowInLog] public bool IsLocalBuild { get; set; }

        [ShowInLog] public EnvironmentType Environment { get; set; }

        [ShowInLog] public string[] Components { get; set; }
        [ShowInLog] public string Version { get; set; }

        [ShowInLog] public ActionType Action { get; set; }

        [ShowInLog] public bool IsForce { get; set; }
        public DeploymentConfig DeploymentConfig { get; set; }

        [ShowInLog] public static int CheckSiteMaxRetry => 10;

        public string ClearPwd { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property)]
    class ShowInLogAttribute : Attribute
    {
    }

    public class CheckSite
    {
        public string Name { get; set; }
        public HttpMethod Method { get; set; }
        public Func<HttpContent> Content { get; set; }
        public string Url { get; set; }
        public bool IsOk { get; set; }
    }
}