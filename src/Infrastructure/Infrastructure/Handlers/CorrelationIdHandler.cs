using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using LSG.Core;
using LSG.SharedKernel.Logger;

namespace LSG.Infrastructure.Handlers
{
    public sealed class CorrelationIdHandler : DelegatingHandler
    {
        private readonly ILsgLogger _lsgLogger;

        public CorrelationIdHandler(
            ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Headers.Contains(Const.Correlation.Header))
                return base.SendAsync(request, cancellationToken);
            
            if (_lsgLogger.CorrelationId == Guid.Empty)
                _lsgLogger.CorrelationId = Guid.NewGuid();

            request.Headers.Add(Const.Correlation.Header, _lsgLogger.CorrelationId.ToString());

            return base.SendAsync(request, cancellationToken);
        }
    }
}