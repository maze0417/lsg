using Microsoft.Extensions.DependencyInjection;

namespace LSG.SharedKernel.Elk
{
    public static class ServiceCollectionExtensions
    {
        public static void AddElk(this IServiceCollection services)
        {
            services.AddSingleton<IElkConfig, ElkConfig>();
            services.AddSingleton<IElkManager, ElkManager>();
        }
    }
}