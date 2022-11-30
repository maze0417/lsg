using System.Net;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Middleware
{
    public sealed class CaughtUnhandledExceptionMiddleware : IMiddleware
    {
        private readonly ILsgLogger _lsgLogger;


        public CaughtUnhandledExceptionMiddleware(ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
        }

        async Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (context.Response.StatusCode != (int) HttpStatusCode.InternalServerError)
            {
                await next(context);
                return;
            }

            context.Response.ContentType = Const.ContentType.JsonContentType;


            var exceptionHandlerPathFeature =
                context.Features.Get<IExceptionHandlerPathFeature>();

            var errorMapper = context.RequestServices.GetRequiredService<IErrorMapper>();
            var apiError = errorMapper
                .GetErrorByException(exceptionHandlerPathFeature.Error);
            var httpCode = errorMapper.GetHttpStatusByException(exceptionHandlerPathFeature.Error);


            _lsgLogger.LogError(Const.SourceContext.CaughtUnhandledException, exceptionHandlerPathFeature.Error,
                exceptionHandlerPathFeature.Error.Message);

            context.Response.StatusCode = (int) httpCode;

            var response = new LsgResponse
            {
                Code = apiError,
                Message = errorMapper.GetMessageByError(apiError, exceptionHandlerPathFeature.Error).message
            };


            await context.Response.WriteAsync(response.ToJson());
        }
    }
}