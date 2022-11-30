using System;
using LSG.Core.Enums;

namespace LSG.Core.Messages.Hub
{
    public abstract class ServerToClientMessage
    {
        public Guid Id { get; set; }

        public string NickName { get; set; }

        public Guid MessageId { get; set; } = Guid.NewGuid();

        public ApiResponseCode Code { get; set; }

        public string Message { get; set; }

        public Guid CorrelationId { get; set; }

        public string GroupName { get; set; }
    }
}