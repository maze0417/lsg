using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core;
using LSG.Core.Messages.Admin;
using LSG.Core.Tokens;
using LSG.Hosts.LoggerWorker;
using LSG.SharedKernel.Elk;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Elasticsearch;
using Serilog.Parsing;

namespace LSG.IntegrationTests.Elk
{
    [TestFixture]
    public class ElkManagerTests : Base<LoggerWorkerStartup>
    {
        [Test]
        public void CanReadConfigModel()
        {
            var config = DefaultFactory.GetRequiredService<IElkConfig>();
            config.Urls.Should().NotBeNullOrEmpty();
        }

        [Test, Category(Const.TestCategory.LocalOnly)]
        public async Task CanInsertLog()
        {
            await using var sw = new StringWriter();
            ITextFormatter formatter = new ElasticsearchJsonFormatter();

            formatter.Format(new LogEvent(DateTimeOffset.Now, LogEventLevel.Information,
                null, new MessageTemplateParser().Parse("this is test insert log"), new LogEventProperty[0]), sw);

            var str = sw.ToString();
            var logs = new LogEventWrapper
            {
                Timestamp = DateTimeOffset.Now,
                MessageTemplate = "test elk template",
                LogEventString = str
            };

            var manager = DefaultFactory.GetRequiredService<IElkManager>();
            var result = manager.EmitBatchLogs(new[] {logs});

            result.Success.Should().BeTrue();
        }

        [Test, Category(Const.TestCategory.LocalOnly)]
        public async Task CanSearchLog()
        {
            LogDeleteMessage(new DeleteMessageRequest
            {
                MessageId = Guid.NewGuid(),
                TokenData = new AdminTokenData(Guid.NewGuid(), Guid.NewGuid(), "admin", "admin", DateTime.UtcNow),
                RoomId = "test room"
            }, playerExternalId: "testplayer", message: "test message", messageTime: DateTimeOffset.Now.AddMinutes(-1));
            var manager = DefaultFactory.GetRequiredService<IElkManager>();

            var query = new Dictionary<string, object>
            {
                {
                    "query",
                    new Dictionary<string, object>
                    {
                        {
                            "bool", new
                            {
                                must = new
                                {
                                    match = new Dictionary<string, object>
                                    {
                                        {
                                            "fields.SourceContext",
                                            "DeleteMessageLog"
                                        }
                                    }
                                },
                                filter = new
                                {
                                    range = new Dictionary<string, object>
                                    {
                                        {
                                            "@timestamp",
                                            new
                                            {
                                                gte = DateTimeOffset.Parse("2021-03-18T10:20:00.0885746+08:00"),
                                                lt = DateTimeOffset.Parse("2021-03-19T10:26:00.0885746+08:00")
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            var host = DefaultFactory.GetRequiredService<IHostEnvironment>();
            var r = await manager.SearchAsync(new SearchRequest
            {
                Index = $"{host.EnvironmentName}*".ToLower(),
                Query = query
            });


            Console.WriteLine(r.ToJson());
        }

        private void LogDeleteMessage(DeleteMessageRequest request, string playerExternalId, string message,
            DateTimeOffset messageTime)
        {
            if (request == null) return;

            var lsgLogger = DefaultFactory.GetRequiredService<ILsgLogger>();
            lsgLogger
                .ForContext(Const.DeleteMessage.PlayerExternalId, playerExternalId)
                .ForContext(Const.DeleteMessage.Message, message)
                .ForContext(Const.DeleteMessage.Operator, request.TokenData.ExternalId)
                .ForContext(Const.DeleteMessage.MessageTime, messageTime)
                .ForContext(Const.DeleteMessage.OperatorTime, DateTimeOffset.Now)
                .LogInformation(Const.SourceContext.DeleteMessageLog,
                    $"[{request.RoomId}] {request.TokenData.ExternalId} delete {playerExternalId} message");
        }
    }
}