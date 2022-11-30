using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using LSG.Core;
using LSG.SharedKernel.Extensions;

namespace LSG.Infrastructure
{
    public abstract class BaseHttpApiClient
    {
        private readonly HttpClient _client;
        private readonly string _serverBaseUrl;
        private readonly IHttpApiClientLogger _httpApiClientLogger;
        private readonly TimeSpan _timeout;
        private readonly string _loggerName;

        protected BaseHttpApiClient(string serverBaseUrl, IHttpClientFactory clientFactory,
            string loggerName,
            IHttpApiClientLogger httpApiClientLogger, TimeSpan? timeout = null)
        {
            _httpApiClientLogger = httpApiClientLogger;
            _loggerName = loggerName;
            _client = clientFactory.CreateClient(Const.Correlation.HttpClient);
            _timeout = timeout ?? TimeSpan.FromSeconds(30);
            _serverBaseUrl = serverBaseUrl;
        }

        protected async Task<TResponse> ExecuteFormRequestAsync<TRequest, TResponse>(
            string path, TRequest request, IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null)
        {
            var fullUrl = _serverBaseUrl.ToUrl(path);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, fullUrl)
            {
                Content = request.ToFormRequest()
            };


            var response = await SendAsync(requestMessage);
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonSerializer.Deserialize<TResponse>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot parse JSON from {json ?? "null input"}", ex);
            }
        }

        protected async Task<TResponse> ExecuteJsonRequestAsync<TRequest, TResponse>(
            HttpMethod method, string path, TRequest request,
            string authToken = null,
            IReadOnlyDictionary<string, string> headers = null,
            IReadOnlyDictionary<string, string> contentHeaders = null)
        {
            var fullUrl =
                method == HttpMethod.Get
                    ? _serverBaseUrl.ToUrl(path, PrepareFormRequest(request))
                    : _serverBaseUrl.ToUrl(path);

            var requestMessage = new HttpRequestMessage(method, fullUrl);
            PrepareRequest(requestMessage, request, headers, contentHeaders);

            if (authToken.IsNotNullOrEmpty() && requestMessage.Headers != null)
            {
                requestMessage.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", authToken);
            }

            var response = await SendAsync(requestMessage);
            var json = await response.Content.ReadAsStringAsync();
            try
            {
                return JsonSerializer.Deserialize<TResponse>(json);
            }
            catch (Exception ex)
            {
                throw new InvalidDataException($"Cannot parse JSON from {json ?? "null input"}", ex);
            }
        }

        private static string PrepareFormRequest(object request)
        {
            return request.ToQueryString();
        }


        private static void PrepareRequest(
            HttpRequestMessage httpRequestMessage, object request,
            IReadOnlyDictionary<string, string> headers,
            IReadOnlyDictionary<string, string> contentHeaders)
        {
            PrepareJsonRequest(httpRequestMessage, request);
            AddHeaders(httpRequestMessage, headers, contentHeaders);


            static void PrepareJsonRequest(HttpRequestMessage req, object content)
            {
                req.Headers.Remove("Accept");
                req.Headers.Add("Accept", "application/json");

                if (content == null) return;
                if (req.Method == HttpMethod.Get) return;


                req.Content = new StringContent(content.ToJson());
                req.Content.Headers.Remove("Content-Type");
                req.Content.Headers.Add("Content-Type", "application/json");
            }


            static void AddHeaders(HttpRequestMessage req, IReadOnlyDictionary<string, string> headers,
                IReadOnlyDictionary<string, string> contentHeaders)
            {
                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        if (req.Headers.Contains(key))
                        {
                            req.Headers.Remove(key);
                        }

                        req.Headers.Add(key, value);
                    }
                }

                if (contentHeaders == null) return;
                foreach (var (key, value) in contentHeaders)
                {
                    if (req.Content.Headers.Contains(key))
                    {
                        req.Content.Headers.Remove(key);
                    }

                    req.Content.Headers.Add(key, value);
                }
            }
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            if (_httpApiClientLogger == null)
            {
                return await SendCoreAsync(request);
            }

            var requestContent = request.Content != null ? await request.Content.ReadAsStringAsync() : string.Empty;
            var sw = Stopwatch.StartNew();
            var requestTime = DateTimeOffset.UtcNow;
            try
            {
                var response = await SendCoreAsync(request);
                sw.Stop();
                await _httpApiClientLogger.LogHttpResponseAsync(response, (int) sw.ElapsedMilliseconds, requestContent,
                    _loggerName, requestTime);

                return response;
            }
            catch (TaskCanceledException ex)
            {
                sw.Stop();

                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(
                        $"Connect to remote host {_serverBaseUrl} timeout {Environment.NewLine}{Environment.NewLine}{ex}"),
                    RequestMessage = request
                };
                await _httpApiClientLogger.LogHttpResponseAsync(response, (int) sw.ElapsedMilliseconds, requestContent,
                    _loggerName, requestTime);
                throw;
            }
            catch (Exception ex)
            {
                sw.Stop();
                var error =
                    $"Internal exception to {_serverBaseUrl} {Environment.NewLine}{Environment.NewLine}{ex}";
                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content =
                        new StringContent(error),
                    RequestMessage = request
                };


                await _httpApiClientLogger.LogHttpResponseAsync(response, (int) sw.ElapsedMilliseconds,
                    requestContent,
                    _loggerName, requestTime);

                throw;
            }
        }

        private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request)
        {
            using var cts = new CancellationTokenSource(_timeout);
            // ReSharper disable once AsyncConverter.AsyncAwaitMayBeElidedHighlighting
            return await _client.SendAsync(request, cts.Token);
        }
    }
}