using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages;
using LSG.Infrastructure.Extensions;
using LSG.Infrastructure.Filters;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using Microsoft.IO;

namespace LSG.Infrastructure.Middleware
{
    public sealed class HttpCallLoggerMiddleware : IMiddleware
    {
        private readonly ILsgLogger _logger;
        private readonly IClientIpAnalyzer _clientIpAnalyzer;
        private readonly IErrorMapper _errorMapper;
        private const int ResMessageSizeLimit = 1000012;

        //avoid LOH :https://stackoverflow.com/questions/43403941/how-to-read-asp-net-core-response-body/52328142#52328142
        private readonly RecyclableMemoryStreamManager _recyclableMemoryStreamManager;


        public HttpCallLoggerMiddleware(
            ILsgLogger logger, IClientIpAnalyzer clientIpAnalyzer, IErrorMapper errorMapper)
        {
            _logger = logger;
            _clientIpAnalyzer = clientIpAnalyzer;
            _errorMapper = errorMapper;
            _recyclableMemoryStreamManager = new RecyclableMemoryStreamManager();
        }

        async Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var hasCorrelationIdHeader =
                context.Request.Headers.TryGetValue(Const.Correlation.Header, out var cid) &&
                !StringValues.IsNullOrEmpty(cid);


            var correlationId = hasCorrelationIdHeader ? cid.FirstOrDefault() : null;

            if (Guid.TryParse(correlationId, out var trackingId) && trackingId != Guid.Empty)
            {
                _logger.CorrelationId = trackingId;
            }
            else
            {
                _logger.CorrelationId = Guid.NewGuid();
            }

            // apply the correlation ID to the response header for client side
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(Const.Correlation.Header))
                {
                    context.Response.Headers.Add(Const.Correlation.Header, _logger.CorrelationId.ToString());
                }

                return Task.CompletedTask;
            });
            _logger.ForContext(Const.Correlation.Id, _logger.CorrelationId);

            var requestPath = context.Request.Path.Value;


            if (!requestPath.StartsWith("/api/"))
            {
                await next(context);
                return;
            }


            var endpoint = context.GetEndpoint();

            if (endpoint?.Metadata.GetMetadata<SuppressHttpCallLogAttribute>() != null)
            {
                await next(context);
                return;
            }


            var requestTime = DateTimeOffset.UtcNow;

            var reqMessage = await context.GetRequestBodyAsync(_recyclableMemoryStreamManager);

            var (resMessage, ms, ex) = await GetResponseMessageAsync(context, next);


            var path = context.Request.GetEncodedUrl();
            var dic = new Dictionary<string, object>
            {
                {"ClientIP", _clientIpAnalyzer.GetIp(context).NoLongerThan(64)},
                {"RequestPath", path.NoLongerThan(512)},
                {"RequestMethod", context.Request.Method.NoLongerThan(8)},
                {"RequestHeaders", context.Request.Headers.ToDetailString()},
                {"RequestContent", reqMessage},
                {"ResponseHeaders", context.Response.Headers.ToDetailString()},
                {
                    "ResponseContent", resMessage?.Length > ResMessageSizeLimit
                        ? CutOffResponseContent(resMessage)
                        : resMessage
                },
                {"StatusCode", context.Response.StatusCode},
                {"ResponseTime", DateTimeOffset.UtcNow},
                {"Elapsed", ms}
            };


            _logger.LogApi(Const.SourceContext.Http, requestTime, dic, ex);
        }

        private async Task<(string resMessage, int elapsedMilliseconds, Exception ex)>
            GetResponseMessageAsync(
                HttpContext context,
                RequestDelegate next)
        {
            var originalBodyStream = context.Response.Body;
            var responseStream = _recyclableMemoryStreamManager.GetStream();
            context.Response.Body = responseStream;
            var sw = Stopwatch.StartNew();
            try
            {
                await next(context);
                sw.Stop();
                var ms = (int) sw.ElapsedMilliseconds;
                var message = await GetBodyMessageAsync(context, originalBodyStream);
                return (message, ms, default);
            }
            catch (Exception ex)
            {
                sw.Stop();
                context.Response.Body = originalBodyStream;
                var ms = (int) sw.ElapsedMilliseconds;

                _logger.LogError(Const.SourceContext.HttpHandledError, ex, ex.Message);
                var apiError = _errorMapper
                    .GetErrorByException(ex);
                var httpCode = _errorMapper.GetHttpStatusByException(ex);

                context.Response.Clear();
                context.Response.StatusCode = (int) httpCode;
                context.Response.ContentType = Const.ContentType.JsonContentType;

                var response = new LsgResponse
                {
                    Code = apiError,
                    Message = _errorMapper.GetMessageByError(apiError, ex).message
                };
                var message = response.ToJson();


                await context.Response.WriteAsync(message);

                return (message, ms, ex);
            }
            finally
            {
                await responseStream.DisposeAsync();
            }
        }

        private static async Task<string> GetBodyMessageAsync(HttpContext context, Stream originalBodyStream)
        {
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(context.Response.Body);
            var message = await sr.ReadToEndAsync();
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
            return message;
        }


        private static string CutOffResponseContent(string responseContent)
        {
            return
                $"{responseContent.NoLongerThan(ResMessageSizeLimit)} ... Response content is too long. Length: {responseContent.Length}";
        }
    }
}