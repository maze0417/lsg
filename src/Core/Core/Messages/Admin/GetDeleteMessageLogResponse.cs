using System;

namespace LSG.Core.Messages.Admin
{
    public class GetDeleteMessageLogResponse : LsgResponse
    {
        public DeleteMessage[] DeleteMessages { get; set; }
    }

    public class DeleteMessage
    {
        public string PlayerExternalId { get; set; }
        public DateTimeOffset OperatorTime { get; set; }
        public DateTimeOffset MessageTime { get; set; }
        public string Operator { get; set; }
        public string Message { get; set; }

        public string RoomId { get; set; }
    }
}