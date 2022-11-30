using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using LSG.Core.Messages;
using LSG.Core.Messages.ServerInfo;
using LSG.Infrastructure;
using LSG.SharedKernel.Elk;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Nats;
using LSG.SharedKernel.Redis;

namespace LSG.Hosts.LoggerWorker;

public sealed class LoggerWorkerServerStatusReporter : BaseServerStatusReporter
{
    private readonly IElkConfig _elkConfig;
    
    public LoggerWorkerServerStatusReporter(ILsgConfig lsgConfig, IHttpClientFactory httpClientFactory,
        INatsManager natsManager, IRedisConnection redisConnection, IElkConfig elkConfig) : base(lsgConfig,
        httpClientFactory, natsManager, redisConnection)
    {
        _elkConfig = elkConfig;
    }


    protected override async Task<BaseServerInfo[]> GetServersInfoAsync()
    {
        return new BaseServerInfo[] { await GetElkAsync() };
    }

    private async Task<ElkInfo> GetElkAsync()
    {
        var url = _elkConfig.Urls.First();
        try
        {
            var response = await ConnectToSiteAsync<ElkServerInfo>(url, string.Empty);

            return new ElkInfo
            {
                Host = url.AbsoluteUri,
                IsConnected = !string.IsNullOrEmpty(response.cluster_name),
                Message = "ok"
            };
        }
        catch (Exception ex)
        {
            return new ElkInfo
            {
                Host = url.AbsoluteUri,
                IsConnected = false,
                Message = ex.GetMessageChain()
            };
        }
    }
}