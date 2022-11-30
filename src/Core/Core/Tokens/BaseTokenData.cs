using System;
using LSG.Core.Enums;

namespace LSG.Core.Tokens
{
    public abstract class BaseTokenData
    {
        public abstract TokenKind Kind { get; }
        public abstract Guid TokenId { get; }

        public abstract DateTime CreatedOn { get; }
    }
}