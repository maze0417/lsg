using System;
using LSG.Core.Enums;

namespace LSG.Core.Tokens
{
    public sealed class AdminTokenData : UserTokenData
    {
        public AdminTokenData(
            Guid tokenId,
            Guid id,
            string name,
            string externalId,
            DateTime createdOn)
        {
            TokenId = tokenId;
            UserIdentifier = id;
            CreatedOn = createdOn;
            Name = name;
            ExternalId = externalId;
        }

        public override TokenKind Kind => TokenKind.Admin;

        public override Guid TokenId { get; }


        public override DateTime CreatedOn { get; }

        public override Guid UserIdentifier { get; }

        public override string Name { get; }


        public override string ExternalId { get; }
    }
}