using System;
using LSG.Core;
using LSG.SharedKernel.Logger;
using StackExchange.Redis;

namespace LSG.SharedKernel.Redis
{
    public abstract class BaseConnectionFactory : IDisposable
    {
        protected readonly Lazy<ConnectionMultiplexer> LazyConnection;

        private readonly ILsgLogger _lsgLogger;

        protected BaseConnectionFactory(string connectionString, ILsgLogger lsgLogger)
        {
            _lsgLogger = lsgLogger;

            LazyConnection = new Lazy<ConnectionMultiplexer>(
                () =>
                {
                    var connection = ConnectionMultiplexer.Connect(connectionString);
                    _lsgLogger.LogInformation(Const.SourceContext.Redis,
                        $"{nameof(ConnectionMultiplexer)} connection opened.");

                    connection.ConnectionFailed += OnConnectionFailed;
                    connection.ConnectionRestored += ConnectionRestored;
                    connection.ErrorMessage += ErrorMessage;
                    connection.InternalError += InternalError;

                    return connection;
                });
        }


        void IDisposable.Dispose()
        {
            if (LazyConnection.IsValueCreated)
            {
                LazyConnection.Value.Dispose();
                _lsgLogger.LogInformation(Const.SourceContext.Redis,
                    $"{nameof(ConnectionMultiplexer)} connection closed.");
            }
        }

        private void OnConnectionFailed(object sender, ConnectionFailedEventArgs e)
        {
            _lsgLogger.LogWarning(Const.SourceContext.Redis, $@"{nameof(ConnectionMultiplexer)} connection failed.
FailureType is {e.FailureType}.
ConnectionType is {e.ConnectionType}.
Endpoint is {e.EndPoint}.
Exception is {e.Exception?.ToString() ?? "null"}");
        }

        private void ConnectionRestored(object sender, ConnectionFailedEventArgs e)
        {
            _lsgLogger.LogInformation(Const.SourceContext.Redis, $@"{nameof(ConnectionMultiplexer)} connection restored.
FailureType is {e.FailureType}.
ConnectionType is {e.ConnectionType}.
Endpoint is {e.EndPoint}.
Exception is {e.Exception?.ToString() ?? "null"}");
        }

        private void ErrorMessage(object sender, RedisErrorEventArgs e)
        {
            _lsgLogger.LogError(Const.SourceContext.Redis, new Exception(e.Message),
                $@"{nameof(ConnectionMultiplexer)} got an error message: {e.Message}.
Endpoint is {e.EndPoint}.");
        }

        private void InternalError(object sender, InternalErrorEventArgs e)
        {
            _lsgLogger.LogError(Const.SourceContext.Redis, e.Exception,
                $@"{nameof(ConnectionMultiplexer)} internal error.
ConnectionType is {e.ConnectionType}.
Endpoint is {e.EndPoint}.
Origin is {e.Origin}.
Exception is {e.Exception?.ToString() ?? "null"}");
        }
    }
}