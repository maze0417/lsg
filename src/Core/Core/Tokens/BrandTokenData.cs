using System;
using LSG.Core.Enums;

namespace LSG.Core.Tokens
{
    public sealed class BrandTokenData : BaseTokenData
    {
        public BrandTokenData(Guid tokenId, Guid brandId, DateTime createdOn)
        {
            BrandId = brandId;
            CreatedOn = createdOn;
            TokenId = tokenId;
        }

        public Guid BrandId { get; }
        public override DateTime CreatedOn { get; }
        public override TokenKind Kind => TokenKind.Brand;

        public override Guid TokenId { get; }
    }
}