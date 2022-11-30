using System;
using System.Net.Http;
using System.Threading.Tasks;
using LSG.Infrastructure;
using LSG.SharedKernel.Extensions;

namespace LSG.IntegrationTests.Utils
{
    public sealed class TestConsoleClientLogger : IHttpApiClientLogger
    {
        Task IHttpApiClientLogger.LogHttpResponseAsync(HttpResponseMessage response, int executionTimeMs,
            string requestContent,
            string prefix, DateTimeOffset requestTime)
        {
            return response.WriteToConsole(requestContent);
        }
    }
}