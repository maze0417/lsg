using System;
using LSG.Core.Tokens;

namespace LSG.Core.Messages.Admin
{
    public class GetDeleteMessageLogRequest : AdminTokenRequest
    {
        public DateTimeOffset? StartMessageTime { get; set; }
        public DateTimeOffset? EndMessageTime { get; set; }

        public DateTimeOffset? StartOperatorTime { get; set; }
        public DateTimeOffset? EndOperatorTime { get; set; }

        public int? Size { get; set; }
    }
}