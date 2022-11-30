using System;
using LSG.Core.Tokens;

namespace LSG.Core.Messages.Admin
{
    public class UnblockUserRequest : AdminTokenRequest
    {
        public Guid PlayerId { get; set; }
    }
}