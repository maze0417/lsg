using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LSG.Core;

namespace LSG.IntegrationTests.Utils
{
    public sealed class UserAgentHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.Contains(Const.ForwardHeaders.UserAgent))
                return base.SendAsync(request, cancellationToken);

            request.Headers.Add(Const.ForwardHeaders.UserAgent, $"{Const.ForwardHeaders.UserAgent}-integrationtest");


            return base.SendAsync(request, cancellationToken);
        }
    }
}