using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using LSG.Core;
using LSG.Core.Messages.Events;
using LSG.SharedKernel.Logger;

namespace LSG.Infrastructure.HostServices
{
    public interface IInternalEventSubject<T> : IDisposable where T : BaseEvent
    {
        void Publish(T message);
        void Subscribe(Func<T, Task> func);
    }

    public sealed class InternalEventSubject<T> : IInternalEventSubject<T> where T : BaseEvent
    {
        private readonly Subject<T> _messageSource = new Subject<T>();
        private readonly IMessageEnrich _messageEnrich;
        private readonly ILsgLogger _lsgLogger;

        public InternalEventSubject(IMessageEnrich messageEnrich, ILsgLogger lsgLogger)
        {
            _messageEnrich = messageEnrich;
            _lsgLogger = lsgLogger;
        }

        void IInternalEventSubject<T>.Publish(T message)
        {
            _lsgLogger
                .LogDebug(Const.SourceContext.InternalEventSubject,
                    "Publish internal {type} : {@message}", typeof(T), message);

            _messageSource.OnNext(message);
        }

        void IInternalEventSubject<T>.Subscribe(Func<T, Task> func)
        {
            _messageSource.AsObservable()
                .Select(message =>
                {
                    message = _messageEnrich.EnrichInternalEvent(message);
                    _lsgLogger
                        .LogDebug(Const.SourceContext.InternalEventSubject,
                            "Receive internal {type} : {@message}", typeof(T), message);
                    return Observable.FromAsync(() => func(message));
                })
                .Concat()
                .Subscribe(message => { });
        }

        void IDisposable.Dispose()
        {
            _messageSource.OnCompleted();
            _messageSource?.Dispose();
        }
    }
}