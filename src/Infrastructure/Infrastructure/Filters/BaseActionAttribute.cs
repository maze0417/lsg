using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LSG.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class BaseActionAttribute : ActionFilterAttribute
    {
        private const string Prefix = "_lsg_scope";
        private const string ActionArguments = "ActionArguments";

        protected bool IsRequestInitiated(HttpRequest request)
        {
            return request.HttpContext.Items.ContainsKey(Prefix + GetType().FullName);
        }

        protected void SetRequestInitiated(HttpRequest request)
        {
            request.HttpContext.Items[Prefix + GetType().FullName] = true;
        }

        protected static void StoreActionArgument(ActionExecutingContext context)
        {
            context.HttpContext.Items[Prefix + ActionArguments] = context.ActionArguments;
        }
    }
}