using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.IO;

namespace LSG.Infrastructure.Extensions
{
    public static class HttpContextExtensions
    {
        public static async ValueTask<string> GetRequestBodyAsync(this HttpContext context,
            RecyclableMemoryStreamManager memoryStreamManager)
        {
            context.Request.EnableBuffering();

            await using var requestStream = memoryStreamManager.GetStream();
            await context.Request.Body.CopyToAsync(requestStream);
            requestStream.Seek(0, SeekOrigin.Begin);
            using var sr = new StreamReader(requestStream);
            var message = await sr.ReadToEndAsync();
            context.Request.Body.Seek(0, SeekOrigin.Begin);
            return message;
        }
    }
}