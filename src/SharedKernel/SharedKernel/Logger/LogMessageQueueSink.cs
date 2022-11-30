using System;
using System.IO;
using LSG.Core;
using LSG.SharedKernel.Nats;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace LSG.SharedKernel.Logger
{
    public sealed class LogMessageQueueSink : ILogEventSink
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ITextFormatter _formatter;

        public LogMessageQueueSink(IServiceProvider serviceProvider, ITextFormatter formatter)
        {
            _serviceProvider = serviceProvider;
            _formatter = formatter;
        }

        void ILogEventSink.Emit(LogEvent logEvent)
        {
            var natsManager = _serviceProvider.GetRequiredService<INatsManager>();


            using var sw = new StringWriter();

            _formatter.Format(logEvent, sw);

            natsManager.Publish(Const.Nats.LogEventTopic, new LogEventWrapper
            {
                Timestamp = logEvent.Timestamp,
                MessageTemplate = logEvent.MessageTemplate.Text,
                LogEventString = sw.ToString()
            });
        }
    }

    public static class LoggerConfigurationExtensions
    {
        public static LoggerConfiguration MessageQueue(
            this LoggerSinkConfiguration loggerConfiguration, IServiceProvider serviceProvider,
            ITextFormatter formatProvider)
        {
            return loggerConfiguration.Sink(new LogMessageQueueSink(serviceProvider, formatProvider));
        }
    }
}