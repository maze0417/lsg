using System.Linq;
using System.Threading.Tasks;
using LSG.Core.Exceptions;
using LSG.Core.Tokens;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Filters
{
    public sealed class AuthorizeBrandAttribute : BeforeActionAttribute
    {
        protected override Task BeforeActionAsync(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var values))
                throw new MissingTokenException("Missing Authorization Bearer Header");

            var authorization = values.FirstOrDefault();
            if (authorization == null || !authorization.StartsWith("Bearer", true, null))
                throw new MissingTokenException("Missing Authorization Bearer Header");

            var token = authorization.Substring(6).Trim();

            if (token.IsNullOrEmpty())
            {
                throw new InvalidTokenException("Empty brand token");
            }


            var tokenProvider = context.HttpContext.RequestServices.GetRequiredService<ITokenProvider>();

            var data = tokenProvider.DecryptAndValidateBrandToken(token);

            var tokenRequest =
                context.ActionArguments.Values
                    .OfType<ITokenRequest<BrandTokenData>>()
                    .FirstOrDefault();
            if (tokenRequest == null)
            {
                throw new InvalidTokenException($"can not cast to brand token data");
            }

            tokenRequest.TokenData = data;
            tokenRequest.RawToken = token;
            var tokenExpiration = context.HttpContext.RequestServices.GetRequiredService<ITokenExpiration>();

            if (tokenExpiration.IsBrandTokenExpired(data.CreatedOn))
            {
                throw new ExpiredTokenException("brand token has expired");
            }

            return Task.CompletedTask;
        }
    }
}