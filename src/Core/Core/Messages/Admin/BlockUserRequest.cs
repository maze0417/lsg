using System;
using LSG.Core.Tokens;

namespace LSG.Core.Messages.Admin
{
    public class BlockUserRequest : AdminTokenRequest
    {
        public int? DurationSeconds { get; set; }
        public Guid PlayerId { get; set; }
    }

    public class BlockUserResponse : LsgResponse
    {
        public BlockUserInfo[] Result { get; set; }
    }

    public class BlockUserInfo
    {
        public Guid PlayerId { get; set; }
        public string PlayerExternalId { get; set; }
        public DateTimeOffset? ExpiredTime { get; set; }
    }
}