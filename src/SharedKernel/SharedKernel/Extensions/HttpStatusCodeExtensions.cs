using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace LSG.SharedKernel.Extensions
{
    public static class HttpStatusCodeExtensions
    {
        public static IActionResult CreateJsonResponse<T>(this HttpStatusCode statusCode, T value)
            where T : class, new()
        {
            return new JsonResult(value)
            {
                StatusCode = (int) statusCode
            };
        }
    }
}