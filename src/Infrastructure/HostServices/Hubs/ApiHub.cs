using System;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Interfaces;
using LSG.Core.Messages.Hub;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.AspNetCore.SignalR;

namespace LSG.Infrastructure.HostServices.Hubs;

// [Authorize(Policy = Const.AuthenticationSchemes.UserToken)]
public sealed class ApiHub : BaseHub
{
    private readonly INotifier _notifier;


    public ApiHub(Func<Type, INotifier> notifierFactory,
        ITokenProvider tokenProvider,
        IErrorMapper errorMapper,
        ILsgLogger lsgLogger,
        ILsgConfig lsgConfig,
        IMessageEnrich messageEnrich 
    ) :
        base(notifierFactory, tokenProvider, errorMapper, lsgLogger, lsgConfig, messageEnrich)
    {
        _notifier = notifierFactory(typeof(ServerNotifier));
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Client(Context.ConnectionId).SendAsync(Const.Hub.Channels.ReceiveMessage, "pong");

        await base.OnConnectedAsync();
    }

    [HubMethodName(Const.Hub.Channels.SendMessage)]
    public Task SendMessageAsync(ClientToServerMessage message)
    {
        return ExecuteAsync(Const.Hub.Channels.SendMessage, async token =>
        {
            if (message.Body.IsNullOrEmpty())
                return;

            if (message.Body.IgnoreCaseEquals("ping"))
                await Clients.Client(Context.ConnectionId).SendAsync(Const.Hub.Channels.ReceiveMessage, "pong");
        });
    }
}