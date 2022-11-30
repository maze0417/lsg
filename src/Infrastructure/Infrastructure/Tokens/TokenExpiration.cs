using System;

namespace LSG.Infrastructure.Tokens
{
    public interface ITokenExpiration
    {
        int GetPlayerTokenExpirationSeconds();
        bool IsPlayerTokenExpired(DateTime utcTokenCreationDate);

        int GetBrandTokenExpirationSeconds();

        bool IsBrandTokenExpired(DateTime utcTokenCreationDate);

        bool IsAnchorTokenExpired(DateTime utcTokenCreationDate);

        bool IsAdminTokenExpired(DateTime utcTokenCreationDate);
    }

    public sealed class TokenExpiration : ITokenExpiration
    {
        private const int PlayerTokenExpirationSeconds = 3600 * 24; // 1d
        private const int BrandTokenExpirationSeconds = 3600; // 1h
        private const int AnchorTokenExpirationSeconds = 3600 * 24; // 1d
        private const int AdminTokenExpirationSeconds = 3600 * 24; // 1d

        int ITokenExpiration.GetPlayerTokenExpirationSeconds()
        {
            return PlayerTokenExpirationSeconds;
        }

        bool ITokenExpiration.IsPlayerTokenExpired(DateTime utcTokenCreationDate)
        {
            return DateTime.UtcNow >= utcTokenCreationDate.AddSeconds(PlayerTokenExpirationSeconds);
        }

        int ITokenExpiration.GetBrandTokenExpirationSeconds()
        {
            return BrandTokenExpirationSeconds;
        }

        bool ITokenExpiration.IsBrandTokenExpired(DateTime utcTokenCreationDate)
        {
            return DateTime.UtcNow >= utcTokenCreationDate.AddSeconds(BrandTokenExpirationSeconds);
        }

        bool ITokenExpiration.IsAnchorTokenExpired(DateTime utcTokenCreationDate)
        {
            return DateTime.UtcNow >= utcTokenCreationDate.AddSeconds(AnchorTokenExpirationSeconds);
        }

        bool ITokenExpiration.IsAdminTokenExpired(DateTime utcTokenCreationDate)
        {
            return DateTime.UtcNow >= utcTokenCreationDate.AddSeconds(AdminTokenExpirationSeconds);
        }
    }
}