using System.Net.Http;

namespace LSG.Infrastructure.Clients;

public interface ILsgFrontendClient
{
}

public sealed class LsgFrontendClient : BaseHttpApiClient, ILsgFrontendClient
{
    public LsgFrontendClient(IHttpClientFactory clientFactory, ILsgConfig lsgConfig,
        IHttpApiClientLogger httpApiClientLogger) : base(
        lsgConfig.LsgFrontendUrl.AbsoluteUri, clientFactory, nameof(LsgFrontendClient), httpApiClientLogger)
    {
    }
}