using LSG.SharedKernel.AppConfig;
using Microsoft.Extensions.Configuration;

namespace LSG.SharedKernel.Redis
{
    public interface IRedisConfig
    {
        string RedisConnectionString { get; }
        string SignalRRedisConnectionString { get; }
    }

    public sealed class RedisConfig : BaseAppConfig, IRedisConfig
    {
        string IRedisConfig.RedisConnectionString => Get("Redis:RedisConnectionString", string.Empty);

        string IRedisConfig.SignalRRedisConnectionString => Get("Redis:SignalRRedisConnectionString", string.Empty);

        public RedisConfig(IConfiguration configuration) : base(configuration)
        {
        }
    }
}