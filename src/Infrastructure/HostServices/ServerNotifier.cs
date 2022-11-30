using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Interfaces;
using LSG.Core.Messages.Hub;
using LSG.SharedKernel.Logger;
using LSG.SharedKernel.Nats;

namespace LSG.Infrastructure.HostServices;

public sealed class ServerNotifier : INotifier
{
    private readonly ILsgLogger _lsgLogger;
    private readonly INatsManager _natsManager;

    public ServerNotifier(INatsManager natsManager, ILsgLogger lsgLogger)
    {
        _natsManager = natsManager;
        _lsgLogger = lsgLogger;
    }


    Task INotifier.NotifyUserExpiredAsync(UserExpiredMessage message)
    {
        return PublishToServerAsync(message);
    }


    private Task PublishToServerAsync<T>(T model, [CallerMemberName] string memberName = "")
        where T : ServerToClientMessage
    {
        model.CorrelationId = _lsgLogger.CorrelationId;
        _natsManager.Publish(typeof(T).Name, model);

        _lsgLogger
            .LogDebug(Const.SourceContext.ServerNotifier,
                "{MemberName} to sever {@model}", memberName, model);
        return Task.CompletedTask;
    }
}