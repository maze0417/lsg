using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LSG.Infrastructure.Filters
{
    public sealed class HandleLsgErrorAttribute : AfterActionAttribute
    {
        protected override Task AfterActionAsync(ActionExecutedContext resultContext)
        {
            if (resultContext.Exception == null || resultContext.ExceptionHandled)
            {
                return Task.CompletedTask;
            }

            resultContext.ExceptionHandled = true;

            var provider = resultContext.HttpContext.RequestServices;

            var errorMapper = provider.GetRequiredService<IErrorMapper>();
            var apiError = errorMapper.GetErrorByException(resultContext.Exception);
            var httpCode = errorMapper.GetHttpStatusByException(resultContext.Exception);

            var logger = provider.GetRequiredService<ILsgLogger>();


            logger.LogError(Const.SourceContext.ActionHandledError, resultContext.Exception,
                resultContext.Exception.Message);

            var env = provider.GetRequiredService<IHostEnvironment>();

            resultContext.Result = httpCode.CreateJsonResponse(new LsgResponse
            {
                Code = apiError,
                Message = env.IsDevelopment()
                    ? $"Dev only => :{resultContext.Exception}"
                    : errorMapper.GetMessageByError(apiError, resultContext.Exception).message
            });
            return Task.CompletedTask;
        }
    }
}