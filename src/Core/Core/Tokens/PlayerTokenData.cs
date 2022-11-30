using System;
using LSG.Core.Enums;

namespace LSG.Core.Tokens
{
    public sealed class PlayerTokenData : UserTokenData
    {
        public PlayerTokenData(
            Guid tokenId,
            Guid playerId,
            Guid brandId,
            string name,
            string externalId,
            DateTime createdOn)
        {
            TokenId = tokenId;
            UserIdentifier = playerId;
            CreatedOn = createdOn;
            Name = name;
            BrandId = brandId;
            ExternalId = externalId;
        }

        public override TokenKind Kind => TokenKind.Player;
        public override Guid TokenId { get; }

        public override Guid UserIdentifier { get; }

        public override string Name { get; }

        public override string ExternalId { get; }
        public Guid BrandId { get; }

        public override DateTime CreatedOn { get; }
    }
}