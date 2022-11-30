using System.Net.Http;
using System.Threading.Tasks;
using LSG.Core.Messages.Player;

namespace LSG.Infrastructure.Clients
{
    public interface IUserApiClient
    {
        Task<LoginPlayerResponse> LoginPlayerAsync(string token, LoginPlayerRequest request);
    }

    public sealed class UserApiClient : BaseHttpApiClient, IUserApiClient
    {
        public UserApiClient(IHttpClientFactory clientFactory, ILsgConfig lsgConfig,
            IHttpApiClientLogger httpApiClientLogger) : base(
            lsgConfig.LsgApiUrl.AbsoluteUri, clientFactory, nameof(UserApiClient), httpApiClientLogger)
        {
        }


        public Task<LoginPlayerResponse> LoginPlayerAsync(string token, LoginPlayerRequest request)
        {
            return ExecuteJsonRequestAsync<LoginPlayerRequest, LoginPlayerResponse>(
                HttpMethod.Post, "/api/player/login", request, token);
        }
    }
}