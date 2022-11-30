using System;
using System.Linq;
using Builds.Deployment.Builds;
using Nuke.Common;
using Nuke.Common.Utilities;

namespace Builds.Deployment
{
    class Program : NukeBuild
    {
        public static int Main(string[] args)
        {
            if (IsDockerBuild())
            {
                return Execute<DockerBuild>(x => x.Finish);
            }

            bool IsDockerBuild()
            {
                return args.Any(a => a.IndexOf(nameof(DockerBuild), StringComparison.OrdinalIgnoreCase) >= 0);
            }

            throw new Exception(
                $"Must specify build i.e. -{nameof(DockerBuild)},the parameter is {args.JoinComma()}");
        }
    }
}