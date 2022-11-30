using System.Net.Http;
using System.Threading.Tasks;
using LSG.Core.Messages.Auth;
using LSG.Core.Messages.Player;

namespace LSG.Infrastructure.Clients
{
    public interface IBrandApiClient
    {
        Task<TokenResponse> GetTokenAsync(ClientCredentialsTokenRequest request);

        Task<AuthorizePlayerResponse> AuthorizePlayerAsync(AuthorizePlayerRequest request, string brandToken);

        Task<PlayerBalanceResponse> GetBalanceAsync(PlayerBalanceRequest request, string brandToken);
    }

    public sealed class BrandApiClient : BaseHttpApiClient, IBrandApiClient
    {
        public BrandApiClient(IHttpClientFactory clientFactory, ILsgConfig lsgConfig,
            IHttpApiClientLogger httpApiClientLogger) : base(
            lsgConfig.UgsConfig.UgsApiUrl.AbsoluteUri, clientFactory, nameof(BrandApiClient),
            httpApiClientLogger)
        {
        }

        Task<TokenResponse> IBrandApiClient.GetTokenAsync(ClientCredentialsTokenRequest request)
        {
            return ExecuteFormRequestAsync<ClientCredentialsTokenRequest, TokenResponse>("/api/oauth/token", request);
        }


        Task<AuthorizePlayerResponse> IBrandApiClient.AuthorizePlayerAsync(AuthorizePlayerRequest request,
            string brandToken)
        {
            return ExecuteJsonRequestAsync<AuthorizePlayerRequest, AuthorizePlayerResponse>(
                HttpMethod.Post, "/api/player/authorize", request, brandToken);
        }

        Task<PlayerBalanceResponse> IBrandApiClient.GetBalanceAsync(PlayerBalanceRequest request, string brandToken)
        {
            return ExecuteJsonRequestAsync<PlayerBalanceRequest, PlayerBalanceResponse>(
                HttpMethod.Get, "/api/player/balance", request, brandToken);
        }
    }
}