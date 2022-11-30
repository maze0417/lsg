using LSG.Core;
using Microsoft.Extensions.Hosting;

namespace LSG.SharedKernel.Extensions
{
    public static class HostEnvironmentEnvExtensions
    {
        public static bool IsIntegration(this IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsEnvironment(Const.Environments.Integration);
        }

        public static bool IsIntegrationTest(this IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsEnvironment(Const.Environments.IntegrationTest);
        }

        public static bool IsDevOrIntOrTest(this IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsDevelopment() || hostEnvironment.IsIntegration() ||
                   hostEnvironment.IsIntegrationTest();
        }

        public static bool IsDevOrTest(this IHostEnvironment hostEnvironment)
        {
            return hostEnvironment.IsDevelopment() || hostEnvironment.IsIntegrationTest();
        }

        public static bool IsNotProduction(this IHostEnvironment hostEnvironment)
        {
            return !hostEnvironment.IsProduction();
        }
    }
}