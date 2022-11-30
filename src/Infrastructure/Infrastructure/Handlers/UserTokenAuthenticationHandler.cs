using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using LSG.Core.Exceptions;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LSG.Infrastructure.Handlers
{
    public sealed class UserTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly ITokenProvider _tokenProvider;


        public UserTokenAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder, ISystemClock clock, ITokenProvider tokenProvider) : base(
            options, logger, encoder,
            clock)
        {
            _tokenProvider = tokenProvider;
        }


        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var values))
            {
                throw new MissingTokenException();
            }

            var authorization = values.FirstOrDefault();
            if (authorization == null || !authorization.StartsWith("Bearer", true, null))
            {
                throw new MissingTokenException("Missing Authorization Bearer Header");
            }


            var token = AuthenticationHeaderValue.Parse(authorization);
            if (token.Parameter.IsNullOrEmpty())
            {
                throw new InvalidTokenException("Empty Authorization Token");
            }


            var tokenData = _tokenProvider.DecryptAndValidateUserToken(token.Parameter);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, tokenData.UserIdentifier.ToString()),
                new Claim(ClaimTypes.Name, tokenData.Name),
                new Claim(ClaimTypes.UserData, token.Parameter)
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}