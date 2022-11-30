using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Reflection;

namespace LSG.Core;

public static class Const
{
    public const string NotAvailable = "NotAvailable";
    public const string ApplicationName = "Lsg";
    public const string StatusKey = "4e5461aa-5e68-411c-8395-fecb65460825";

    public static readonly string ServerName =
        Environment.GetEnvironmentVariable(Environments.AspnetcoreServerName) ?? Environment.MachineName;

    public static readonly Assembly EntryAssembly = Assembly.GetEntryAssembly();

    public static readonly string Version = EntryAssembly.GetName().Version?.ToString();

    public static class Correlation
    {
        public const string Header = "x-correlation-id";
        public const string HttpClient = "CorrelationClient";
        public const string Id = "CorrelationId";
    }


    public static class Sites
    {
        public const string LsgApi = "LsgApi";
        public const string LoggerWorker = "LoggerWorker";
        public const string LsgFrontend = "LsgFrontend";
    }

    public static class Hub
    {
        public const string ApiHubPath = "/ws";


        public static class Channels
        {
            public const string SendMessage = "SendMessage";
            public const string ReceiveMessage = "ReceiveMessage";


            public const string ChatHistory = "ChatHistory";
            public const string ReceiveChatMessage = "ReceiveChatMessage";
            public const string DeleteChatMessage = "DeleteChatMessage";
            public const string Activity = "Activity";
            public const string AddGroup = "AddGroup";

            public const string UserExpired = "UserExpired";

            public const string PlayerLobbyConnect = "PlayerLobbyConnect";
            public const string PlayerLogged = "PlayerLogged";
        }
    }

    public static class AuthenticationSchemes
    {
        public const string UserToken = "AuthUserTokenScheme";
    }

    public static class ColumnTypes
    {
        public const string Varchar = "VARCHAR";
        public const string NvarcharMax = "NVARCHAR(MAX)";
        public const string Nvarchar = "NVARCHAR";
        public const string Decimal = "decimal(18,6)";
    }

    public static class TestCategory
    {
        public const string LocalOnly = "LocalOnly";
        public const string AppConfig = "AppConfig";
        public const string Integration = "Integration";
        public const string Factory = "Factory";
        public const string Wip = "Wip";
    }

    public static class ConnectionStringNames
    {
        public const string Lsg = "Lsg";
        public const string LsgReadOnly = "LsgReadOnly";
    }

    public static class Environments
    {
        public const string AspnetcoreServerName = "ASPNETCORE_SERVERNAME";
        public const string AspnetcoreEnvironment = "ASPNETCORE_ENVIRONMENT";
        public const string AspnetcoreHostingStartupAssemblies = "ASPNETCORE_HOSTINGSTARTUPASSEMBLIES";

        public const string Integration = "Integration";
        public const string Development = "Development";
        public const string IntegrationTest = "IntegrationTest";

        public static readonly HashSet<string> All = new()
        {
            AspnetcoreServerName,
            AspnetcoreEnvironment,
            AspnetcoreHostingStartupAssemblies
        };
    }

    public static class ContentType
    {
        public static readonly string JsonContentType = new MediaTypeHeaderValue("application/json").ToString();
    }

    public static class ConfigSectionName
    {
        public const string UgsConfig = "UgsConfig";
        public const string AdminConfig = "AdminConfig";
    }

    public static class SourceContext
    {
        public const string Name = "SourceContext";
        public const string CaughtUnhandledException = nameof(CaughtUnhandledException);
        public const string Redis = nameof(Redis);
        public const string ActionHandledError = nameof(ActionHandledError);
        public const string NatsSource = "Nats";
        public const string RunHost = "RunHost";
        public const string Http = "Http";
        public const string HttpHandledError = nameof(HttpHandledError);
        public const string ServerNotifier = nameof(ServerNotifier);
        public const string ClientAppNotifier = nameof(ClientAppNotifier);
        public const string SignalrHub = nameof(SignalrHub);
        public const string TransactionManager = nameof(TransactionManager);
        public const string DataSeeder = nameof(DataSeeder);
        public const string InternalEventSubject = nameof(InternalEventSubject);
        public const string DeleteMessageLog = nameof(DeleteMessageLog);
    }

    public static class Nats
    {
        public const string LogEventTopic = "LogEventTopic";
        public const string LogQueue = "LogQueue";
    }

    public static class GameEventTopic
    {
        public const string ServerNotifyUgsGameRoundStatisticMessage =
            nameof(ServerNotifyUgsGameRoundStatisticMessage);
    }

    public static class ForwardHeaders
    {
        public const string UserAgent = "User-Agent";
    }


    public static class DeleteMessage
    {
        public const string PlayerExternalId = "PlayerExternalId";
        public const string MessageTime = "MessageTime";
        public const string Message = "Message";
        public const string Operator = "Operator";
        public const string OperatorTime = "OperatorTime";
        public const string RoomId = "RoomId";
    }


    public static class UgsGameSetting
    {
        public const int DefaultBetLimitId = 1;
        public const string DefaultPlayerLanguage = "zh-CN";
    }
}