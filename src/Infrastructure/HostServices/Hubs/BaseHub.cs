using System;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Enums;
using LSG.Core.Interfaces;
using LSG.Core.Messages.Hub;
using LSG.Core.Tokens;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.SignalR;

namespace LSG.Infrastructure.HostServices.Hubs;

public abstract class BaseHub : Hub
{
    private readonly INotifier _broadCastNotifier;

    private readonly IErrorMapper _errorMapper;
    private readonly ILsgConfig _lsgConfig;
    private readonly ILsgLogger _lsgLogger;
    private readonly ITokenProvider _tokenProvider;
    internal readonly IMessageEnrich MessageEnrich;

    protected BaseHub(
        Func<Type, INotifier> notifierFactory,
        ITokenProvider tokenProvider,
        IErrorMapper errorMapper,
        ILsgLogger lsgLogger,
        ILsgConfig lsgConfig,
        IMessageEnrich messageEnrich
    )
    {
        _tokenProvider = tokenProvider;
        _errorMapper = errorMapper;
        _lsgLogger = lsgLogger;
        _lsgConfig = lsgConfig;
        MessageEnrich = messageEnrich;


        _broadCastNotifier = notifierFactory(typeof(ServerNotifier));
    }

    protected async Task ExecuteAsync(string channel, Func<UserTokenData, Task> func)
    {
        var tokenData = GetTokenData();

        try
        {
            await func(tokenData);
        }
        catch (Exception e)
        {
            var code = _errorMapper.GetErrorByException(e);
            var (statusCode, message) = _errorMapper.GetMessageByError(code, e);

            _lsgLogger.LogError(Const.SourceContext.SignalrHub, e, message);

            var response = new
            {
                Code = code,
                Message = message,
                Id = tokenData.UserIdentifier,
                NickName = tokenData.Name,
                _lsgLogger.CorrelationId
            };
            if (statusCode == HttpStatusCode.Unauthorized)
            {
                var data = MessageEnrich.EnrichServerToClientMessage(new UserExpiredMessage(),
                    tokenData, ApiResponseCode.ExpiredOrUnauthorizedToken);

                await _broadCastNotifier.NotifyUserExpiredAsync(data);
                return;
            }

            await Clients.Caller.SendAsync(channel, response);

            if (_lsgConfig.Environment.IsDevOrIntOrTest()) throw new HubException(message, e);
        }
    }


    protected UserTokenData GetTokenData()
    {
        if (Context.User == null) throw new UnauthorizedAccessException();
        var rawToken = Context.User.FindFirst(ClaimTypes.UserData)?.Value;
        return _tokenProvider.DecryptAndValidateUserToken(rawToken);
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }
}