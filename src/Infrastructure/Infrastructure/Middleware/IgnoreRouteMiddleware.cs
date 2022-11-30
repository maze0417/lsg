using System.Linq;
using System.Net;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Enums;
using LSG.Core.Messages;
using LSG.Infrastructure.Filters;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Middleware
{
    public sealed class IgnoreRouteMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var endpoint = context.GetEndpoint();

            var whiteList = endpoint?.Metadata.GetMetadata<RouteAcceptWhenSiteIsAttribute>();
            if (whiteList == null)
            {
                await next(context);
                return;
            }

            var site = context.RequestServices.GetRequiredService<ILsgConfig>().CurrentSite;

            if (whiteList.Sites.Any(a => a.IgnoreCaseEquals(site)))
            {
                await next.Invoke(context);
                return;
            }


            context.Response.StatusCode = 404;
            var errorMapper = context.RequestServices.GetRequiredService<IErrorMapper>();
            var result = new LsgResponse
            {
                Code = ApiResponseCode.IgnoredRout,
                Message = errorMapper
                    .GetMessageByError(ApiResponseCode.IgnoredRout, null, statusCode: HttpStatusCode.NotFound).message
            };

            context.Response.ContentType = Const.ContentType.JsonContentType;
            await context.Response.WriteAsync(result.ToJson());
        }
    }
}