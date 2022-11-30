using LSG.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace LSG.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void UseNotFoundHandler(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<NotFoundMiddleware>();
        }

        public static void UseCatchAllExceptionHandler(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<CaughtUnhandledExceptionMiddleware>();
        }

        public static void UseHttpCallLogger(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<HttpCallLoggerMiddleware>();
        }


        public static void UseWebSocketAuthentication(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<WebSocketAuthenticationMiddleware>();
        }

        public static void UseIgnoreRoute(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<IgnoreRouteMiddleware>();
        }
    }
}