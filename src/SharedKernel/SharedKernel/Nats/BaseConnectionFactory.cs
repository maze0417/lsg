using System;
using LSG.Core;
using LSG.SharedKernel.Logger;
using NATS.Client;

namespace LSG.SharedKernel.Nats
{
    public abstract class BaseConnectionFactory : IDisposable
    {
        protected readonly Lazy<IConnection> LazyConnection;
        private readonly ILsgLogger _lsgLogger;


        protected BaseConnectionFactory(INatsConfig natsConfig, ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;
            LazyConnection = new Lazy<IConnection>(() =>
            {
                var opts = ConnectionFactory.GetDefaultOptions();
                opts.Servers = natsConfig.Urls;
                opts.MaxReconnect = Options.ReconnectForever;
                opts.ReconnectedEventHandler += ReconnectedEventHandler;
                opts.DisconnectedEventHandler += DisconnectedEventHandler;
                opts.ClosedEventHandler += ClosedEventHandler;
                opts.AsyncErrorEventHandler += AsyncErrorEventHandler;
                return new ConnectionFactory().CreateConnection(opts);
            });
        }

        private void ReconnectedEventHandler(object sender, ConnEventArgs e)
        {
            _lsgLogger.LogWarning(Const.SourceContext.NatsSource, $"Reconnected to NATS. {e.Conn.Opts.Url}");
        }

        private void DisconnectedEventHandler(object sender, ConnEventArgs e)
        {
            _lsgLogger.LogWarning(Const.SourceContext.NatsSource, $"Disconnected to NATS. {e.Conn.Opts.Url}");
        }

        private void ClosedEventHandler(object sender, ConnEventArgs e)
        {
            _lsgLogger.LogWarning(Const.SourceContext.NatsSource, $"NATS connection closed. {e.Conn.Opts.Url}");
        }

        private void AsyncErrorEventHandler(object sender, ErrEventArgs e)
        {
            _lsgLogger.LogWarning(Const.SourceContext.NatsSource,
                $"Error occurred. Subject:{e.Subscription.Subject} Error:{e.Error}");
        }

        public void Dispose()
        {
            if (!LazyConnection.IsValueCreated)
                return;
            LazyConnection.Value?.Drain();
            LazyConnection.Value?.Dispose();
        }
    }
}