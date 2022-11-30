using System;

namespace LSG.Core.Tokens
{
    public abstract class UserTokenData : BaseTokenData
    {
        public abstract Guid UserIdentifier { get; }

        public abstract string Name { get; }


        public abstract string ExternalId { get; }
    }
}