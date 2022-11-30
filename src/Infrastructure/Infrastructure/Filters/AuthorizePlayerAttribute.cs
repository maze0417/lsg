using System.Linq;
using System.Threading.Tasks;
using LSG.Core.Enums;
using LSG.Core.Exceptions;
using LSG.Core.Interfaces;
using LSG.Core.Messages.Hub;
using LSG.Core.Tokens;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Filters
{
    public sealed class AuthorizePlayerAttribute : BeforeActionAttribute
    {
        protected override async Task BeforeActionAsync(ActionExecutingContext context)
        {
            if (!context.HttpContext.Request.Headers.TryGetValue("Authorization", out var values))
                throw new MissingTokenException("Missing Authorization Bearer Header");

            var authorization = values.FirstOrDefault();
            if (authorization == null || !authorization.StartsWith("Bearer", true, null))
                throw new MissingTokenException("Missing Authorization Bearer Header");

            var token = authorization.Substring(6).Trim();

            if (token.IsNullOrEmpty())
            {
                throw new InvalidTokenException("Empty player token");
            }


            var tokenProvider = context.HttpContext.RequestServices.GetRequiredService<ITokenProvider>();

            var tokenData = tokenProvider.DecryptAndValidatePlayerToken(token);


            if (context.ActionArguments.Values.Count != 0)
            {
                var tokenRequest =
                    context.ActionArguments.Values
                        .OfType<ITokenRequest<PlayerTokenData>>()
                        .FirstOrDefault();
                if (tokenRequest == null)
                {
                    throw new InvalidTokenException("can not cast to player token data");
                }

                tokenRequest.TokenData = tokenData;
                tokenRequest.RawToken = token;
            }

            var tokenExpiration = context.HttpContext.RequestServices.GetRequiredService<ITokenExpiration>();

            if (tokenExpiration.IsPlayerTokenExpired(tokenData.CreatedOn))
            {
                var notifier = context.HttpContext.RequestServices.GetRequiredService<INotifier>();
                var enrich = context.HttpContext.RequestServices.GetRequiredService<IMessageEnrich>();

                var data = enrich.EnrichServerToClientMessage(new UserExpiredMessage(),
                    tokenData, ApiResponseCode.ExpiredOrUnauthorizedToken);
                await notifier.NotifyUserExpiredAsync(data);
                
                throw new ExpiredTokenException("brand token has expired");
            }
        }
    }
}