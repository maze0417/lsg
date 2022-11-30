using Microsoft.Extensions.DependencyInjection;

namespace LSG.SharedKernel.Nats
{
    public static class ServiceCollectionExtensions
    {
        public static void AddNats(this IServiceCollection services)
        {
            services.AddSingleton<INatsConfig, NatsConfig>();
            services.AddSingleton<INatsConnection, NatsConnection>();
            services.AddSingleton<INatsManager, NatsManager>();
        }
    }
}