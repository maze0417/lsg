using System;
using LSG.Core;
using LSG.Hosts.LsgApi;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Extensions.Hosting;
using Serilog.Parsing;
using Serilog.Sinks.SystemConsole.Themes;

namespace LSG.IntegrationTests
{
    [TestFixture]
    public class SeriLogTests : Base<LsgApiStartup>
    {
        [Test]
        public void CanLogError()
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("App", "123")
                .WriteTo.Console(
                    outputTemplate:
                    "{Timestamp:HH:mm:ss} [{EventType:x8} {Level:u3}]-[{SourceContext}] {Message:lj}{Properties:j}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            var ex = new Exception("test ex");
            var mesage = $"error occured {ex.Message}";


            logger.Error(ex, "{Message} => {env}", mesage, Const.SourceContext.CaughtUnhandledException);

            LogContext.PushProperty("EventType", Const.SourceContext.CaughtUnhandledException);
            logger.Error(ex, mesage);

            var testClass = new {Name = "123", Value = 456};

            logger.Information("this is test class {@ab}", testClass);
        }


        [Test]
        public void CanUseDiagnosticContext()
        {
            var logger = DefaultFactory.GetRequiredService<ILogger>();


            var diag = new DiagnosticContext(logger);

            var collector = diag.BeginCollection();
            LogContext.PushProperty("RequestId", "resutid-1234");
            diag.Set("RequestPath", "/api/test");

            if (!collector.TryComplete(out var collectedProperties,out _))
                Assert.Fail();


            const string defaultRequestCompletionMessageTemplate =
                "HTTP[{RequestId}] {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";


            var messageTemplate =
                new MessageTemplateParser().Parse(
                    defaultRequestCompletionMessageTemplate);
            var evt = new LogEvent(DateTimeOffset.Now, LogEventLevel.Information, null, messageTemplate,
                collectedProperties);
            logger.ForContext<SeriLogTests>().Write(evt);
        }
    }
}