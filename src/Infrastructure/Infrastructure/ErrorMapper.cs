using System;
using System.Collections.Generic;
using System.Net;
using LSG.Core.Enums;
using LSG.Core.Exceptions;
using LSG.SharedKernel.Extensions;
using Microsoft.Extensions.Hosting;

namespace LSG.Infrastructure;

public interface IErrorMapper
{
    ApiResponseCode GetErrorByException(Exception exception);
    HttpStatusCode GetHttpStatusByException(Exception exception);

    (HttpStatusCode statusCode, string message)
        GetMessageByError(ApiResponseCode responseCode, Exception ex,
            HttpStatusCode statusCode = HttpStatusCode.InternalServerError);
}

public sealed class ErrorMapper : IErrorMapper
{
    private static readonly IDictionary<Type, ApiResponseCode> ErrorByException =
        new Dictionary<Type, ApiResponseCode>
        {
            { typeof(ExpiredLiveStreamException), ApiResponseCode.ExpiredLiveStreamToken },

            { typeof(ExpiredTokenException), ApiResponseCode.ExpiredOrUnauthorizedToken },
            { typeof(TestPlayerNotSupportException), ApiResponseCode.NotSupportTestPlayer },
            { typeof(UserCacheNotExistException), ApiResponseCode.UserCacheNotExist },
            { typeof(ExpiredLiveClientException), ApiResponseCode.LiveClientCacheExpired },
            { typeof(UnauthorizedAccessException), ApiResponseCode.ExpiredOrUnauthorizedToken },
            { typeof(ArgumentException), ApiResponseCode.InvalidArguments },
            { typeof(ArgumentNullException), ApiResponseCode.InvalidArguments },
            { typeof(FormatException), ApiResponseCode.IncorrectFormat },
            { typeof(InvalidTokenException), ApiResponseCode.IncorrectFormat },
            { typeof(MissingTokenException), ApiResponseCode.MissingToken },
            { typeof(TimeoutException), ApiResponseCode.ConfiguredTimeoutExceeded },
            { typeof(InvalidOperationException), ApiResponseCode.SystemError },
            { typeof(ModelBindingException), ApiResponseCode.InvalidModelBinding },
            { typeof(OperationNotAllowedException), ApiResponseCode.Forbidden },
            { typeof(InvalidCredentialsException), ApiResponseCode.InvalidCredentials },
            { typeof(BrandUnderMaintenanceException), ApiResponseCode.AnchorOffline },
            { typeof(CurrencyNotSupportException), ApiResponseCode.NotSupportCurrency }
        };

    private static readonly IDictionary<ApiResponseCode, HttpStatusCode> HttpCodeByError =
        new Dictionary<ApiResponseCode, HttpStatusCode>
        {
            { ApiResponseCode.DataNotExist, HttpStatusCode.OK },
            { ApiResponseCode.MissingToken, HttpStatusCode.BadRequest },
            { ApiResponseCode.IncorrectFormat, HttpStatusCode.BadRequest },
            { ApiResponseCode.InvalidModelBinding, HttpStatusCode.BadRequest },
            { ApiResponseCode.NotSupportCurrency, HttpStatusCode.BadRequest },
            { ApiResponseCode.InvalidLiveStreamToken, HttpStatusCode.Unauthorized },
            { ApiResponseCode.ExpiredOrUnauthorizedToken, HttpStatusCode.Unauthorized },
            { ApiResponseCode.NotSupportTestPlayer, HttpStatusCode.Unauthorized },
            { ApiResponseCode.InvalidCredentials, HttpStatusCode.Unauthorized },
            { ApiResponseCode.UserCacheNotExist, HttpStatusCode.Unauthorized },
            { ApiResponseCode.LiveClientCacheExpired, HttpStatusCode.Unauthorized },
            { ApiResponseCode.Forbidden, HttpStatusCode.Forbidden },
            { ApiResponseCode.AnchorOffline, HttpStatusCode.ServiceUnavailable },
            { ApiResponseCode.SystemError, HttpStatusCode.InternalServerError }
        };

    private static readonly IDictionary<HttpStatusCode, string> DefaultErrorMessageMapper =
        new Dictionary<HttpStatusCode, string>
        {
            { HttpStatusCode.BadRequest, "The requested resource is incorrect formant." },
            { HttpStatusCode.NotFound, "The requested resource is not found." },
            { HttpStatusCode.Unauthorized, "The requested resource is unauthorized." },
            { HttpStatusCode.UnsupportedMediaType, "Unsupported request resource." },
            { HttpStatusCode.Forbidden, "Forbidden request resource." },
            { HttpStatusCode.ServiceUnavailable, "Service is under maintenance." },
            { HttpStatusCode.InternalServerError, "Internal system error." }
        };

    private readonly ILsgConfig _lsgConfig;


    public ErrorMapper(ILsgConfig lsgConfig)
    {
        _lsgConfig = lsgConfig;
    }

    ApiResponseCode IErrorMapper.GetErrorByException(Exception exception)
    {
        if (exception == null)
            return ApiResponseCode.Success;

        var type = exception.GetType();
        return ErrorByException.TryGetValue(type, out var error) ? error : ApiResponseCode.SystemError;
    }

    HttpStatusCode IErrorMapper.GetHttpStatusByException(Exception exception)
    {
        if (exception == null)
            return HttpStatusCode.OK;

        var type = exception.GetType();
        if (!ErrorByException.TryGetValue(type, out var error)) return HttpStatusCode.InternalServerError;
        return HttpCodeByError.TryGetValue(error, out var code) ? code : HttpStatusCode.InternalServerError;
    }

    (HttpStatusCode statusCode, string message) IErrorMapper.GetMessageByError(ApiResponseCode responseCode,
        Exception ex, HttpStatusCode statusCode)
    {
        var errorStatus = HttpCodeByError.TryGetValue(responseCode, out var status)
            ? status
            : statusCode;


        var defaultMessage = DefaultErrorMessageMapper.TryGetValue(errorStatus, out var message)
            ? message
            : DefaultErrorMessageMapper[HttpStatusCode.InternalServerError];


        if (_lsgConfig.Environment.IsProduction())
            return (errorStatus, defaultMessage);

        return (errorStatus, ex?.GetMessageChain() ?? defaultMessage);
    }
}