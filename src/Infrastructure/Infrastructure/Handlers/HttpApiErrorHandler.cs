using System;
using System.Threading.Tasks;
using LSG.Infrastructure.Filters;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LSG.Infrastructure.Handlers
{
    public interface IHttpApiErrorHandler
    {
        Task ExpectingErrorActionAsync(
            ActionExecutingContext context,
            Func<ActionExecutingContext, Task> actionAsync,
            ActionExecutionDelegate next,
            Func<Exception, Exception> errorHandler = null);
    }

    public sealed class HttpApiErrorHandler : IHttpApiErrorHandler
    {
        async Task IHttpApiErrorHandler.ExpectingErrorActionAsync(ActionExecutingContext context,
            Func<ActionExecutingContext, Task> actionAsync,
            ActionExecutionDelegate next, Func<Exception, Exception> errorHandler)
        {
            try
            {
                await actionAsync(context);
            }
            catch (Exception ex)
            {
                await OnErrorAsync(context, errorHandler == null ? ex : errorHandler(ex));
            }
        }

        private static async Task OnErrorAsync(ActionExecutingContext context, Exception ex)
        {
            var filters = context.ActionDescriptor.FilterDescriptors;
            var actionExecutedContext = new ActionExecutedContext(context, context.Filters, context.Controller);

            foreach (var filter in filters)
            {
                if (!(filter.Filter is AfterActionAttribute afa)) continue;
                if (afa.IsInitiated(context.HttpContext.Request)) continue;

                actionExecutedContext.Exception = ex;
                afa.OnActionExecuted(actionExecutedContext);
                if (context.Result != null) return;
                await afa.OnActionExecutionAsync(context, () => Task.FromResult(actionExecutedContext));
                context.Result = actionExecutedContext.Result;
            }
        }
    }
}