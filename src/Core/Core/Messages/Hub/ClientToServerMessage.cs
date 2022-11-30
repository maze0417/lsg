namespace LSG.Core.Messages.Hub;

public class ClientToServerMessage
{
    public string Body { get; set; }

    public MessageType Type { get; set; }
}

public enum MessageType : byte
{
    Message = 1,
    Emoji = 2
}