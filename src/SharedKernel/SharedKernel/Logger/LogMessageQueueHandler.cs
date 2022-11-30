using System;
using LSG.Core;
using LSG.SharedKernel.Elk;
using LSG.SharedKernel.Nats;

namespace LSG.SharedKernel.Logger
{
    public interface ILogMessageQueueHandler
    {
        void StartReceiveMessages();

        void StopReceiveMessages();

        DateTimeOffset LastReceivedEventTime { get; set; }
        DateTimeOffset LastLogInsertTime { get; set; }
        string LastLogInsertError { get; set; }
    }

    public sealed class LogMessageQueueHandler : ILogMessageQueueHandler
    {
        private readonly INatsManager _natsManager;
        private IDisposable _natsDisposable;
        private IDisposable _rxDisposable;

        private readonly ILogMessageQueueHandler _this;

        private readonly IElkManager _elkManager;

        public LogMessageQueueHandler(INatsManager natsManager,
            IElkManager elkManager)
        {
            _natsManager = natsManager;
            _elkManager = elkManager;

            _this = this;
        }

        void ILogMessageQueueHandler.StartReceiveMessages()
        {
            _elkManager.OnError += LogLastError;

            (_natsDisposable, _rxDisposable) = _natsManager.SubscribeBatchAsync<LogEventWrapper>(
                Const.Nats.LogEventTopic, logEvents =>
                {
                    _this.LastReceivedEventTime = DateTimeOffset.UtcNow;

                    _elkManager.EmitBatchLogs(logEvents);

                    _this.LastLogInsertTime = DateTimeOffset.UtcNow;
                }, TimeSpan.FromSeconds(1), 50, Const.Nats.LogQueue);
        }

        private void LogLastError(string error)
        {
            _this.LastLogInsertError = error;
        }


        void ILogMessageQueueHandler.StopReceiveMessages()
        {
            _elkManager.OnError -= LogLastError;
            _natsDisposable?.Dispose();
            _rxDisposable?.Dispose();
        }

        DateTimeOffset ILogMessageQueueHandler.LastReceivedEventTime { get; set; }

        DateTimeOffset ILogMessageQueueHandler.LastLogInsertTime { get; set; }

        string ILogMessageQueueHandler.LastLogInsertError { get; set; }
    }
}