using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using LSG.Core;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using MessagePack;
using NATS.Client;

namespace LSG.SharedKernel.Nats
{
    public interface INatsManager
    {
        void Publish<T>(string topic, T content, bool fireForget = false);

        IAsyncSubscription SubscribeAsync<T>(string topic, Action<T> action, string queueName = null);

        (IDisposable natDisposable, IDisposable rxDisposable ) SubscribeBatchAsync<T>(string topic, Action<T[]> action,
            TimeSpan waitTime,
            int batchCount,
            string queueName = null);
        
        (bool isConnected, string url, Exception error) GetConnectionInfo();
    }

    public sealed class NatsManager : INatsManager
    {
        private readonly INatsConnection _natsConnection;
        private readonly ILsgLogger _lsgLogger;
        private readonly INatsManager _this;


        private readonly MessagePackSerializerOptions _messagePackSerializerOptions =
            MessagePack.Resolvers.ContractlessStandardResolver.Options
                .WithCompression(MessagePackCompression.Lz4BlockArray);


        public NatsManager(INatsConnection natsConnection, ILsgLogger lsgLogger)
        {
            _natsConnection = natsConnection;
            _lsgLogger = lsgLogger;
            _this = this;
        }

        (bool isConnected, string url, Exception error) INatsManager.GetConnectionInfo()
        {
            var isConnected = false;
            try
            {
                var conn = _natsConnection.Connection;
                isConnected = conn.State == ConnState.CONNECTED;
                return (isConnected, conn.ConnectedUrl, null);
            }
            catch (Exception e)
            {
                return (isConnected, _natsConnection.Config.Urls.JoinAsStringByComma(), e);
            }

           
        }
        void INatsManager.Publish<T>(string topic, T content, bool fireForget)
        {
            byte[] data;
            try
            {
                data = MessagePackSerializer.Serialize(content, _messagePackSerializerOptions);
            }
            catch (Exception e)
            {
                _lsgLogger.LogConsole(Const.SourceContext.NatsSource,
                    $"Failed to Deserialize {typeof(T).Name} -{e}");
                throw;
            }

            if (!fireForget)
            {
                _natsConnection.Connection.Publish(topic, data);
                return;
            }

            Action action = () => _natsConnection.Connection.Publish(topic, data);
            action.RunAsFireForget();
        }


        IAsyncSubscription INatsManager.SubscribeAsync<T>(string topic, Action<T> action, string queueName)
        {
            return _natsConnection.Connection.SubscribeAsync(topic, queueName,
                (sender, args) =>
                {
                    try
                    {
                        var message =
                            MessagePackSerializer.Deserialize<T>(args.Message.Data, _messagePackSerializerOptions);
                        action?.Invoke(message);
                    }
                    catch (Exception e)
                    {
                        _lsgLogger.LogConsole(Const.SourceContext.NatsSource,
                            $"Failed to Deserialize {typeof(T).Name} {e}");
                        throw;
                    }
                });
        }


        (IDisposable natDisposable, IDisposable rxDisposable ) INatsManager.SubscribeBatchAsync<T>(string topic,
            Action<T[]> action, TimeSpan waitTime, int batchCount, string queueName)
        {
            var receivedMessage = new Subject<T>();
            var nats = _this.SubscribeAsync<T>(topic, message => { receivedMessage.OnNext(message); }, queueName);

            var rx = receivedMessage.AsObservable()
                .Buffer(waitTime, batchCount)
                .Where(a => a.Count > 0)
                .SubscribeSafe(Observer.Create<IList<T>>(message => { action?.Invoke(message.ToArray()); }));


            return (nats, rx);
        }
    }
}