using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LSG.Core;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;

namespace LSG.Infrastructure.Handlers
{
    public sealed class ForwardHeadersHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly HashSet<string> ForwardedHeaders = new HashSet<string>
        {
            Const.ForwardHeaders.UserAgent
        };

        public ForwardHeadersHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var headers = _httpContextAccessor.HttpContext?.Request?.Headers;

            if (headers == null)
                return base.SendAsync(request, cancellationToken);

            var joinHeaders = (from h in headers
                from f in ForwardedHeaders
                where h.Key.IgnoreCaseEquals(f)
                select f).ToArray();

            if (!joinHeaders.Any())
            {
                return base.SendAsync(request, cancellationToken);
            }

            foreach (var header in joinHeaders)
            {
                request.Headers.Add(header, headers[header].ToString());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}