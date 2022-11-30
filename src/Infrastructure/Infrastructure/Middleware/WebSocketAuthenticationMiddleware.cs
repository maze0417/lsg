using System;
using System.Threading.Tasks;
using LSG.Core;
using Microsoft.AspNetCore.Http;

namespace LSG.Infrastructure.Middleware;

public sealed class WebSocketAuthenticationMiddleware : IMiddleware
{
    Task IMiddleware.InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;

        // web sockets cannot pass headers so we must take the access token from query param and
        // add it to the header before authentication middleware runs

        var isHubPath = request.Path.StartsWithSegments(Const.Hub.ApiHubPath, StringComparison.OrdinalIgnoreCase);

        if (isHubPath &&
            request.Query.TryGetValue("access_token", out var accessToken))
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

        return next(context);
    }
}