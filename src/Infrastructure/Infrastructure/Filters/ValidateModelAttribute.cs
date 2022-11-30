using System.Linq;
using System.Threading.Tasks;
using LSG.Core.Exceptions;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LSG.Infrastructure.Filters
{
    public class ValidateModelAttribute : BeforeActionAttribute
    {
        protected override Task BeforeActionAsync(ActionExecutingContext context)
        {
            if (context.ModelState.IsValid) return Task.CompletedTask;

            var errors = context.ModelState.Values.SelectMany(
                ms => ms.Errors.Select(e =>
                    string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception.Message : e.ErrorMessage));
            throw new ModelBindingException("Error binding model: " + errors.JoinAsStringByComma());
        }
    }
}