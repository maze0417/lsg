using System;
using LSG.Core.Enums;
using LSG.Core.Exceptions;
using LSG.Core.Tokens;
using LSG.Infrastructure.Security;
using LSG.SharedKernel.Extensions;

namespace LSG.Infrastructure.Tokens;

public interface ITokenProvider
{
    PlayerTokenData DecryptAndValidatePlayerToken(string tokenString, bool throwIfExpired = true);

    string EncryptPlayerToken(PlayerTokenData data);

    BrandTokenData DecryptAndValidateBrandToken(string tokenString, bool throwIfExpired = true);

    string EncryptBrandToken(BrandTokenData data);


    UserTokenData DecryptAndValidateUserToken(string tokenString, bool throwIfExpired = true);

    string EncryptAdminToken(AdminTokenData data);

    AdminTokenData DecryptAndValidateAdminToken(string tokenString, bool throwIfExpired = true);
}

public sealed class TokenProvider : ITokenProvider
{
    private readonly ICryptoProvider _cryptoProvider;
    private readonly ITokenExpiration _expiration;
    private readonly ITokenProvider _this;

    public TokenProvider(ICryptoProvider cryptoProvider, ITokenExpiration expiration)
    {
        _cryptoProvider = cryptoProvider;
        _expiration = expiration;
        _this = this;
    }

    PlayerTokenData ITokenProvider.DecryptAndValidatePlayerToken(string tokenString, bool throwIfExpired)
    {
        PlayerTokenData playerTokenData;
        try
        {
            playerTokenData = DecryptSpecificToken(tokenString, TokenKind.Player, r =>
            {
                var tokenId = r.ReadGuid();
                var playerId = r.ReadGuid();
                var brandId = r.ReadGuid();
                var userName = r.ReadString();
                var externalId = r.ReadString();
                var createdOn = r.ReadUInt();

                if (createdOn == 0) throw new InvalidTokenException($"failed to decrypt token {tokenString}");

                return new PlayerTokenData(tokenId, playerId, brandId, userName, externalId,
                    ((long)createdOn).UnixTimeToUtcDateTime());
            });
        }
        catch (Exception ex)
        {
            throw new InvalidTokenException($"Can't decrypt token {tokenString} =>  {ex.GetMessageChain()}", ex);
        }

        if (throwIfExpired && _expiration.IsPlayerTokenExpired(playerTokenData.CreatedOn))
            throw new ExpiredTokenException("Expired player token");

        if (playerTokenData.UserIdentifier == Guid.Empty)
            throw new InvalidTokenException("player token is invalid because player id is empty");

        return playerTokenData;
    }

    string ITokenProvider.EncryptPlayerToken(PlayerTokenData data)
    {
        var bytes =
            new BinaryStreamWriter()
                .AddByte((byte)data.Kind)
                .AddGuid(data.TokenId)
                .AddGuid(data.UserIdentifier)
                .AddGuid(data.BrandId)
                .AddString(data.Name)
                .AddString(data.ExternalId)
                .AddUInt((uint)data.CreatedOn.ToUnixTimeSeconds())
                .ToBytesArray();
        return _cryptoProvider.EncryptBytes(bytes);
    }

    BrandTokenData ITokenProvider.DecryptAndValidateBrandToken(string tokenString, bool throwIfExpired)
    {
        BrandTokenData brandTokenData;
        try
        {
            brandTokenData = DecryptSpecificToken(tokenString, TokenKind.Brand, r =>
            {
                var tokenId = r.ReadGuid();
                var brandId = r.ReadGuid();
                var createdOn = r.ReadUInt();

                return new BrandTokenData(tokenId, brandId,
                    ((long)createdOn).UnixTimeToUtcDateTime());
            });
        }
        catch (Exception e)
        {
            throw new InvalidTokenException($"Can't decrypt brand token {tokenString} =>  {e.GetMessageChain()}");
        }


        if (throwIfExpired && _expiration.IsBrandTokenExpired(brandTokenData.CreatedOn))
            throw new ExpiredTokenException("Expired brand token");

        if (brandTokenData.BrandId == Guid.Empty)
            throw new InvalidTokenException("brand token is invalid because brand id is empty");

        return brandTokenData;
    }

    string ITokenProvider.EncryptBrandToken(BrandTokenData data)
    {
        var bytes =
            new BinaryStreamWriter()
                .AddByte((byte)data.Kind)
                .AddGuid(data.TokenId)
                .AddGuid(data.BrandId)
                .AddUInt((uint)data.CreatedOn.ToUnixTimeSeconds())
                .ToBytesArray();
        return _cryptoProvider.EncryptBytes(bytes);
    }


    public UserTokenData DecryptAndValidateUserToken(string tokenString, bool throwIfExpired = true)
    {
        var bytes = _cryptoProvider.DecryptBytes(tokenString);

        using var r = new BinaryStreamReader(bytes);

        var kind = (TokenKind)r.ReadByte();


        if (kind == TokenKind.Player)
            return _this.DecryptAndValidatePlayerToken(tokenString, throwIfExpired);

        if (kind == TokenKind.Admin)
            return _this.DecryptAndValidateAdminToken(tokenString, throwIfExpired);

        throw new InvalidTokenException($"token must be player or anchor ,but found {kind}");
    }

    string ITokenProvider.EncryptAdminToken(AdminTokenData data)
    {
        var bytes =
            new BinaryStreamWriter()
                .AddByte((byte)data.Kind)
                .AddGuid(data.TokenId)
                .AddGuid(data.UserIdentifier)
                .AddString(data.Name)
                .AddString(data.ExternalId)
                .AddUInt((uint)data.CreatedOn.ToUnixTimeSeconds())
                .ToBytesArray();
        return _cryptoProvider.EncryptBytes(bytes);
    }

    AdminTokenData ITokenProvider.DecryptAndValidateAdminToken(string tokenString, bool throwIfExpired)
    {
        AdminTokenData tokenData;
        try
        {
            tokenData = DecryptSpecificToken(tokenString, TokenKind.Admin, r =>
            {
                var tokenId = r.ReadGuid();
                var id = r.ReadGuid();
                var userName = r.ReadString();
                var externalId = r.ReadString();
                var createdOn = r.ReadUInt();

                return new AdminTokenData(tokenId, id, userName, externalId,
                    ((long)createdOn).UnixTimeToUtcDateTime());
            });
        }
        catch (Exception ex)
        {
            throw new InvalidTokenException($"Can't decrypt token {tokenString} => {ex.GetMessageChain()}", ex);
        }

        if (throwIfExpired && _expiration.IsAnchorTokenExpired(tokenData.CreatedOn))
            throw new ExpiredTokenException("Expired  token");

        if (tokenData.UserIdentifier == Guid.Empty)
            throw new InvalidTokenException("token is invalid because id is empty");

        return tokenData;
    }

    private T DecryptSpecificToken<T>(string tokenString, TokenKind expectKind, Func<BinaryStreamReader, T> read)
        where T : BaseTokenData
    {
        var bytes = _cryptoProvider.DecryptBytes(tokenString);

        using var r = new BinaryStreamReader(bytes);

        var kind = (TokenKind)r.ReadByte();
        if (kind != expectKind)
            throw new ArgumentException($"The token was expected to be {expectKind} token kind but it was {kind}");

        return read(r);
    }
}