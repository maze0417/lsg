using System;
using System.Threading.Tasks;
using LSG.Infrastructure.Handlers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace LSG.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class BeforeActionAttribute : BaseActionAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            SetRequestInitiated(context.HttpContext.Request);
            StoreActionArgument(context);

            var errorHandler = context.HttpContext.RequestServices.GetRequiredService<IHttpApiErrorHandler>();

            await errorHandler.ExpectingErrorActionAsync(context, BeforeActionAsync, next);

            if (context.Result == null)
            {
                await next();
            }
        }

        protected abstract Task BeforeActionAsync(ActionExecutingContext context);
    }
}