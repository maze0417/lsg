using System;
using System.Net;
using LSG.Core.Enums;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace LSG.Infrastructure
{
    /// <summary>
    /// handle status code 415 and anything else code >400
    /// </summary>
    public sealed class ClientErrorFactory : IClientErrorFactory
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;

        public ClientErrorFactory(IOptions<ApiBehaviorOptions> apiBehaviorOptions)
        {
            _apiBehaviorOptions =
                apiBehaviorOptions?.Value ?? throw new ArgumentNullException(nameof(apiBehaviorOptions));
        }

        IActionResult IClientErrorFactory.GetClientError(ActionContext actionContext,
            IClientErrorActionResult clientError)
        {
            var statusCode = clientError.StatusCode ?? (int) HttpStatusCode.InternalServerError;

            if (_apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var clientErrorData))
            {
                return ((HttpStatusCode) statusCode).CreateJsonResponse(new LsgResponse
                {
                    Code = ApiResponseCode.NotSupport,
                    Message = clientErrorData.Title
                });
            }

            var errorMapper = actionContext.HttpContext.RequestServices.GetRequiredService<IErrorMapper>();
            return ((HttpStatusCode) statusCode).CreateJsonResponse(new LsgResponse
            {
                Code = ApiResponseCode.SystemError,
                Message = errorMapper.GetMessageByError(ApiResponseCode.SystemError, null).message
            });
        }
    }
}