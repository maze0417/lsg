namespace LSG.Core.Messages.ServerInfo;

public class BaseServerInfo
{
    public virtual string ServerType { get; set; }
    public string Host { get; set; }
    public bool IsConnected { get; set; }
    public string Message { get; set; }

    public string Name { get; set; }
}