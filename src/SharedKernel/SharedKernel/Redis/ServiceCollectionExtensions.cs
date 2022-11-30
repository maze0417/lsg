using System;
using LSG.SharedKernel.Logger;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.SharedKernel.Redis
{
    public static class ServiceCollectionExtensions
    {
        public static void AddRedis(this IServiceCollection services)
        {
            services.AddSingleton<IRedisConfig, RedisConfig>();
            services.AddSingleton<RedisConnection>();
            services.AddSingleton<SignalRedisConnection>();
            services.AddSingleton<IRedisConnection, RedisConnection>();
            services.AddSingleton<Func<Type, IRedisConnection>>(provider =>
                key => (IRedisConnection) provider.GetRequiredService(key));

            services.AddSingleton<IRedisCacheManager, RedisCacheManager>();
            services.AddSingleton<Func<Type, IRedisCacheManager>>(provider =>
                key =>
                {
                    var connection = (IRedisConnection) provider.GetRequiredService(key);
                    var logger = provider.GetRequiredService<ILsgLogger>();
                    return new RedisCacheManager(connection, logger);
                });
            services.AddSingleton(typeof(IRedisList<>), typeof(RedisList<>));

            services.AddTransient<IRedisLock, RedisLock>();

            services.AddSingleton<Func<Type, IRedisLock>>(provider => key =>
            {
                var connection = (IRedisConnection) provider.GetRequiredService(key);

                return new RedisLock(connection);
            });
        }
    }
}