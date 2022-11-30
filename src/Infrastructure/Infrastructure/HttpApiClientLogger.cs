using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;

namespace LSG.Infrastructure
{
    public interface IHttpApiClientLogger
    {
        Task LogHttpResponseAsync(HttpResponseMessage response, int executionTimeMs, string requestContent,
            string prefix, DateTimeOffset requestTime);
    }

    public sealed class HttpApiClientLogger : IHttpApiClientLogger
    {
        private readonly ILsgLogger _lsgLogger;

        public HttpApiClientLogger(ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
        }


        async Task IHttpApiClientLogger.LogHttpResponseAsync(HttpResponseMessage response, int executionTimeMs,
            string requestContent, string prefix, DateTimeOffset requestTime)
        {
            var request = response.RequestMessage;
            var resMessage = response.Content != null ? await response.Content.ReadAsStringAsync() : string.Empty;


            var dic = new Dictionary<string, object>
            {
                {"RequestPath", request.RequestUri.AbsoluteUri},
                {"RequestMethod", request.Method.ToString().NoLongerThan(8)},
                {"RequestHeaders", request.Headers + request.Content?.Headers?.ToString()},
                {"RequestContent", requestContent},
                {"ResponseHeaders", response.Headers.ToString()},
                {"ResponseContent", resMessage},
                {"ResponseTime", DateTimeOffset.UtcNow},
                {"StatusCode", (int) response.StatusCode},
                {"Elapsed", executionTimeMs}
            };


            _lsgLogger.LogApi(prefix, requestTime, dic, default);
        }
    }
}