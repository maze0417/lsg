using System;
using System.Collections.Generic;
using System.Linq;
using LSG.Core;
using Serilog.Context;
using Serilog.Core.Enrichers;
using Serilog.Events;
using Serilog.Parsing;
using ISeriLogger = Serilog.ILogger;


namespace LSG.SharedKernel.Logger
{
    public interface ILsgLogger
    {
        Guid CorrelationId { get; set; }

        void LogApi(string sourceContext, DateTimeOffset requestTime, IDictionary<string, object> dictionary,
            Exception ex);

        void LogSocket(string sourceContext, IDictionary<string, object> dictionary,
            LogEventLevel logEventLevel = LogEventLevel.Information);


        void LogError(string sourceContext, Exception ex, string message);

        void LogWarning(string sourceContext, string message, params object[] properties);

        void LogInformation(string sourceContext, string message);
        void LogInformation<T>(string sourceContext, string message, T model);

        ILsgLogger ForContext(string name, object value, bool destructureObjects = false);

        void LogConsole(string sourceContext, string message);
        void LogDebug(string sourceContext, string message, params object[] properties);
    }

    public sealed class LsgLogger : ILsgLogger
    {
        private const string TrackingIdCallContextSlotName = "CallerContext::CorrelationId";
        private const string SocketSenderIdCallContextSlotName = "CallerContext::SocketSenderId";

        private readonly ISeriLogger _logger;
        private readonly MessageTemplateParser _messageTemplateParser = new MessageTemplateParser();
        private static readonly LogEventProperty[] NoProperties = new LogEventProperty[0];
        private readonly ILsgLogger _this;

        public LsgLogger(ISeriLogger logger)
        {
            _logger = logger;
            _this = this;
        }


        public Guid CorrelationId
        {
            get => CallContext<Guid>.GetData(TrackingIdCallContextSlotName);
            set
            {
                var slot = CallContext<Guid>.GetData(TrackingIdCallContextSlotName);
                if (slot == Guid.Empty)
                {
                    CallContext<Guid>.SetData(TrackingIdCallContextSlotName, value);
                }
            }
        }

        public Guid SocketSenderId
        {
            get => CallContext<Guid>.GetData(SocketSenderIdCallContextSlotName);
            set
            {
                var slot = CallContext<Guid>.GetData(SocketSenderIdCallContextSlotName);
                if (slot == Guid.Empty)
                {
                    CallContext<Guid>.SetData(SocketSenderIdCallContextSlotName, value);
                }
            }
        }


        void ILsgLogger.LogApi(string sourceContext, DateTimeOffset requestTime, IDictionary<string, object> dictionary,
            Exception ex)
        {
            _logger
                .ForContext(Const.SourceContext.Name, sourceContext)
                .ForContext(dictionary.Select(d => new PropertyEnricher(d.Key, d.Value)))
                .Write(new LogEvent(requestTime, ex == default ? LogEventLevel.Information : LogEventLevel.Error, ex,
                    _messageTemplateParser.Parse(
                        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms"),
                    NoProperties));
        }


        void ILsgLogger.LogSocket(string sourceContext, IDictionary<string, object> dictionary,
            LogEventLevel logEventLevel)
        {
            if (dictionary == null || dictionary.Count == 0)
            {
                return;
            }


            _logger
                .ForContext(Const.SourceContext.Name, sourceContext)
                .ForContext(dictionary.Select(d => new PropertyEnricher(d.Key, d.Value)))
                .Write(new LogEvent(DateTimeOffset.UtcNow, logEventLevel, null,
                    _messageTemplateParser.Parse(
                        "[{ExternalId}] [{Url}] [{Direction}] [{CmdWithHex}] len({Length}) "),
                    NoProperties));
        }

        void ILsgLogger.LogError(string sourceContext, Exception ex, string message)
        {
            _logger.ForContext(Const.SourceContext.Name, sourceContext).Error(ex, message);
        }

        void ILsgLogger.LogWarning(string sourceContext, string message, params object[] properties)
        {
            _logger.ForContext(Const.SourceContext.Name, sourceContext).Warning(message, properties);
        }

        void ILsgLogger.LogInformation(string sourceContext, string message)
        {
            _logger.ForContext(Const.SourceContext.Name, sourceContext).Information(message);
        }

        void ILsgLogger.LogInformation<T>(string sourceContext, string message, T model)
        {
            _logger.ForContext(Const.SourceContext.Name, sourceContext).Information(message, model);
        }

        ILsgLogger ILsgLogger.ForContext(string name, object value, bool destructureObjects)
        {
            LogContext.PushProperty(name, value, destructureObjects);
            return _this;
        }

        void ILsgLogger.LogConsole(string sourceContext, string message)
        {
            Console.WriteLine($@"{sourceContext}- {message}");
        }

        void ILsgLogger.LogDebug(string sourceContext, string message, params object[] properties)
        {
            _logger.ForContext(Const.SourceContext.Name, sourceContext).Debug(message, properties);
        }
    }
}