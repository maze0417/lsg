using System;
using LSG.SharedKernel.Logger;
using StackExchange.Redis;

namespace LSG.SharedKernel.Redis;

public interface IRedisConnection
{
    ConnectionMultiplexer Connection { get; }

    string ConnectionString { get; }
    (bool isConnected, string url, Exception error) GetConnectionInfo();
}

public sealed class RedisConnection : BaseConnectionFactory, IRedisConnection
{
    private readonly IRedisConnection _this;

    public RedisConnection(IRedisConfig config, ILsgLogger lsgLogger) : base(config.RedisConnectionString,
        lsgLogger)
    {
        _this = this;
        ConnectionString = config.RedisConnectionString;
    }

    ConnectionMultiplexer IRedisConnection.Connection => LazyConnection.Value;

    (bool isConnected, string url, Exception error) IRedisConnection.GetConnectionInfo()
    {
        var isConnected = false;
        try
        {
            var conn = _this.Connection;
            isConnected = conn.IsConnected;
            return (isConnected, ConnectionString, null);
        }
        catch (Exception e)
        {
            return (isConnected, ConnectionString, e);
        }
    }

    public string ConnectionString { get; }
}

public sealed class SignalRedisConnection : BaseConnectionFactory, IRedisConnection
{
    private readonly IRedisConnection _this;

    public SignalRedisConnection(IRedisConfig config, ILsgLogger lsgLogger) : base(
        config.SignalRRedisConnectionString,
        lsgLogger)
    {
        _this = this;
        ConnectionString = config.SignalRRedisConnectionString;
    }

    ConnectionMultiplexer IRedisConnection.Connection => LazyConnection.Value;


    (bool isConnected, string url, Exception error) IRedisConnection.GetConnectionInfo()
    {
        var isConnected = false;
        try
        {
            var conn = _this.Connection;
            isConnected = conn.IsConnected;
            return (isConnected, ConnectionString, null);
        }
        catch (Exception e)
        {
            return (isConnected, ConnectionString, e);
        }
    }

    public string ConnectionString { get; }
}