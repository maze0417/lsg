using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages;
using LSG.Core.Messages.ServerInfo;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Nats;
using LSG.SharedKernel.Redis;

namespace LSG.Infrastructure;

public interface IServerStatusReporter
{
    Task<ServerStatus> GetServerStatusAsync();
}

public abstract class BaseServerStatusReporter : IServerStatusReporter
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILsgConfig _lsgConfig;
    private readonly INatsManager _natsManager;
    private readonly IRedisConnection _redisConnection;

    protected BaseServerStatusReporter(ILsgConfig lsgConfig, IHttpClientFactory httpClientFactory,
        INatsManager natsManager, IRedisConnection redisConnection)
    {
        _lsgConfig = lsgConfig;
        _httpClientFactory = httpClientFactory;
        _natsManager = natsManager;
        _redisConnection = redisConnection;
    }


    async Task<ServerStatus> IServerStatusReporter.GetServerStatusAsync()
    {
        var servers = await GetServersInfoAsync();
        var baseInfo = GetBaseStatus();
        var temp = baseInfo.ServerInfos.ToList();
        temp.AddRange(servers);
        baseInfo.ServerInfos = temp.ToArray();
        return baseInfo;
    }


    protected abstract Task<BaseServerInfo[]> GetServersInfoAsync();


    protected async Task<TResponse> ConnectToSiteAsync<TResponse>(Uri url, string path)
    {
        var client = _httpClientFactory.CreateClient();

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri(url, path)
        };

        var response = await client.SendAsync(request);


        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<TResponse>(content);
    }


    protected ServerStatus GetBaseStatus()
    {
        var redisInfo = _redisConnection.GetConnectionInfo();

        var natsInfo = _natsManager.GetConnectionInfo();


        return new ServerStatus
        {
            Site = _lsgConfig.CurrentSite,
            PhysicalPath = AppDomain.CurrentDomain.BaseDirectory,
            MachineName = Const.ServerName,
            Version = Const.Version,
            EnvironmentVariables =
                Const.Environments.All.ToDictionary(a => a,
                    Environment.GetEnvironmentVariable),
            ServerInfos = new BaseServerInfo[]
            {
                new RedisInfo
                {
                    Host = redisInfo.url,
                    IsConnected = redisInfo.isConnected,
                    Message = redisInfo.error?.GetMessageChain()
                },
                new NatsInfo
                {
                    Host = natsInfo.url,
                    Message = natsInfo.error?.GetMessageChain(),
                    IsConnected = natsInfo.isConnected
                }
            }
        };
    }
}