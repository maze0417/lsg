using System;
using System.Net.Http;
using System.Threading.Tasks;
using LSG.Core.Messages.ServerInfo;
using LSG.Infrastructure;
using LSG.Infrastructure.DataServices;
using LSG.SharedKernel.Nats;
using LSG.SharedKernel.Redis;

namespace LSG.Hosts.LsgFrontend;

public sealed class FrontendStatusReporter : BaseServerStatusReporter
{
    private readonly Func<ILsgReadOnlyRepository> _lsgReadOnlyRepositoryFactory;

    public FrontendStatusReporter(ILsgConfig lsgConfig, IHttpClientFactory clientFactory,
        INatsManager natsManager, IRedisConnection redisConnection,
        Func<ILsgReadOnlyRepository> lsgReadOnlyRepositoryFactory) : base(lsgConfig,
        clientFactory, natsManager, redisConnection)
    {
        _lsgReadOnlyRepositoryFactory = lsgReadOnlyRepositoryFactory;
    }


    protected override Task<BaseServerInfo[]> GetServersInfoAsync()
    {
        return Task.FromResult(new BaseServerInfo[] { _lsgReadOnlyRepositoryFactory().GetConnectionInfo() });
    }
}