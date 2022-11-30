using System;
using LSG.Core.Tokens;

namespace LSG.Core.Messages.Admin
{
    public class DeleteMessageRequest : AdminTokenRequest
    {
        public string RoomId { get; set; }
        public Guid MessageId { get; set; }
    }
}