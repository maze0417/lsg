using System.Net;
using LSG.Core.Enums;
using LSG.Core.Messages;
using LSG.SharedKernel.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace LSG.Infrastructure
{
    public interface IResponseCreator
    {
        IActionResult CreateErrorResponse(ApiResponseCode code, string errorMessage);

        IActionResult CreateOkResponse<T>(T response);

        IActionResult CreateResponse(LsgResponse response);
    }

    public sealed class ResponseCreator : IResponseCreator
    {
        private readonly IErrorMapper _errorMapper;
        private readonly IResponseCreator _this;

        public ResponseCreator(IErrorMapper errorMapper)
        {
            _errorMapper = errorMapper;
            _this = this;
        }

        IActionResult IResponseCreator.CreateErrorResponse(ApiResponseCode code, string errorMessage)
        {
            var (status, message) = _errorMapper.GetMessageByError(code, null);

            return status.CreateJsonResponse(new LsgResponse
            {
                Code = code,
                Message = errorMessage ?? message
            });
        }

        public IActionResult CreateOkResponse<T>(T response)
        {
            return new JsonResult(response)
            {
                StatusCode = (int) HttpStatusCode.OK
            };
        }

        public IActionResult CreateResponse(LsgResponse response)
        {
            return response.Code == ApiResponseCode.Success
                ? _this.CreateOkResponse(response)
                : _this.CreateErrorResponse(response.Code, response.Message);
        }
    }
}