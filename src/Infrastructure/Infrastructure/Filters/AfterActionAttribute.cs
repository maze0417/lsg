using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LSG.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class AfterActionAttribute : BaseActionAttribute
    {
        public bool IsInitiated(HttpRequest request) => IsRequestInitiated(request);

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();
            SetRequestInitiated(resultContext.HttpContext.Request);
            await AfterActionAsync(resultContext);
        }

        protected abstract Task AfterActionAsync(ActionExecutedContext resultContext);
    }
}