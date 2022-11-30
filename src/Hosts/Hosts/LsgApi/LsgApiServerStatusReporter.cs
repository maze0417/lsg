using System;
using System.Net.Http;
using System.Threading.Tasks;
using LSG.Core.Messages.ServerInfo;
using LSG.Infrastructure;
using LSG.Infrastructure.DataServices;
using LSG.SharedKernel.Elk;
using LSG.SharedKernel.Nats;
using LSG.SharedKernel.Redis;

namespace LSG.Hosts.LsgApi;

public sealed class LsgApiServerStatusReporter : BaseServerStatusReporter
{
    private readonly Func<ILsgRepository> _lsgRepositoryFactory;


    public LsgApiServerStatusReporter(ILsgConfig lsgConfig, Func<ILsgRepository> lsgRepositoryFactory,
        IHttpClientFactory httpClientFactory,
        INatsManager natsManager, IRedisConnection redisConnection) : base(lsgConfig,
        httpClientFactory, natsManager, redisConnection)
    {
        _lsgRepositoryFactory = lsgRepositoryFactory;
    }


    protected override Task<BaseServerInfo[]> GetServersInfoAsync()
    {
        return Task.FromResult(new BaseServerInfo[] { _lsgRepositoryFactory().GetConnectionInfo() });
    }
}