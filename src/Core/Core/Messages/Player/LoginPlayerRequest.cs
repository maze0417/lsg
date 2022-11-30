using LSG.Core.Enums;
using LSG.Core.Tokens;

namespace LSG.Core.Messages.Player
{
    public sealed class LoginPlayerRequest : BrandTokenRequest
    {
        public string Name { get; set; }
        public string ExternalId { get; set; }
        public string CultureCode { get; set; }
        public string CurrencyCode { get; set; }
        public PlayerType? Type { get; set; }
        public int? BetLimitGroupId { get; set; }
    }
}