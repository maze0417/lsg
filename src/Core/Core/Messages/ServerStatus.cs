using System.Collections.Generic;
using LSG.Core.Enums;
using LSG.Core.Messages.ServerInfo;

namespace LSG.Core.Messages;

public class ServerStatus
{
    public string Site { get; set; }
    public string PhysicalPath { get; set; }
    public string MachineName { get; set; }
    public string Version { get; set; }


    public IDictionary<string, string> EnvironmentVariables { get; set; }

    public BaseServerInfo[] ServerInfos { get; set; }
}

public class AppServerStatus : ServerStatus
{
}

public class FrontendServerStatus : ServerStatus
{
}

public class NatsInfo : BaseServerInfo
{
    public override string ServerType => ServerInfoType.Nats.ToString();
}

public class DatabaseInfo : BaseServerInfo
{
    public override string ServerType { get; set; } =  ServerInfoType.Database.ToString();
}

public class ElkInfo : BaseServerInfo
{
    public override string ServerType { get; set; } =  ServerInfoType.Elk.ToString();
}

public class RedisInfo : BaseServerInfo
{
    public override string ServerType { get; set; } =  ServerInfoType.Redis.ToString();
}


public class ElkServerInfo
{
    public string name { get; set; }
    public string cluster_name { get; set; }
    public string cluster_uuid { get; set; }
    public string tagline { get; set; }
}