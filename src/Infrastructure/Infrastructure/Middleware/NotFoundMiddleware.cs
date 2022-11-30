using System.Net;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Enums;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Middleware
{
    public sealed class NotFoundMiddleware : IMiddleware
    {
        async Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
        {
            await next(context);

            if (context.Response.HasStarted)
            {
                return;
            }

            if (context.Response.StatusCode == (int) HttpStatusCode.NotFound)
            {
                var errorMapper = context.RequestServices.GetRequiredService<IErrorMapper>();
                var result = new LsgResponse
                {
                    Code = ApiResponseCode.NotFound,
                    Message = errorMapper
                        .GetMessageByError(ApiResponseCode.NotFound, null, statusCode: HttpStatusCode.NotFound).message
                };

                context.Response.ContentType = Const.ContentType.JsonContentType;
                await context.Response.WriteAsync(result.ToJson());
            }
        }
    }
}