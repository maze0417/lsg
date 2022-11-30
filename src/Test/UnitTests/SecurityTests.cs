using System;
using System.Text;
using AutoFixture;
using FluentAssertions;
using LSG.Core.Tokens;
using LSG.Infrastructure.Security;
using LSG.Infrastructure.Tokens;
using LSG.SharedKernel.Extensions;
using NUnit.Framework;

namespace LSG.UnitTests;

[TestFixture]
public class SecurityTests
{
    [SetUp]
    public void Init()
    {
        _cryptoProvider = new CryptoProvider(new Base62DataEncoder());
        _tokenProvider = new TokenProvider(new CryptoProvider(new Base62DataEncoder()), new TokenExpiration());
    }

    private ICryptoProvider _cryptoProvider;
    private ITokenProvider _tokenProvider;

    [TestCase("120")]
    [TestCase("love爱")]
    public void Encoded_CanBe_Decoded(string input)
    {
        IDataEncoder converter = new Base62DataEncoder();
        var encoded = converter.Encode(Encoding.UTF8.GetBytes(input));

        var decoded = converter.Decode(encoded);

        input.Should().Be(Encoding.UTF8.GetString(decoded));
    }

    [Test]
    [Repeat(10000)]
    public void Can_encrypt_and_decrypt_string()
    {
        var initialString = new Fixture().Create<string>();


        var encryptedString = _cryptoProvider.EncryptBytes(Encoding.UTF8.GetBytes(initialString));
        var decryptedString = Encoding.UTF8.GetString(_cryptoProvider.DecryptBytes(encryptedString));

        encryptedString.Should().NotBeNullOrWhiteSpace();
        encryptedString.Should().NotBe(initialString);
        decryptedString.Should().NotBeNullOrWhiteSpace();
        decryptedString.Should().Be(initialString);
    }

    [Test]
    [Repeat(10000)]
    public void CanEncryptAndDecryptPlayerToken()
    {
        var input = new PlayerTokenData(
            Guid.NewGuid(), Guid.NewGuid(), new Guid("40848AA9-20CA-4B4B-8C0A-08D7F4B92A4C"),
            "HTTW10", "TSTHTTW10", DateTime.UtcNow);


        var output = _tokenProvider.EncryptPlayerToken(input);

        var current = _tokenProvider.DecryptAndValidatePlayerToken(output);

        current.TokenId.Should().Be(input.TokenId);
        current.UserIdentifier.Should().Be(input.UserIdentifier);
        current.Name.Should().Be(input.Name);
        current.ExternalId.Should().Be(input.ExternalId);
        Math.Abs(current.CreatedOn.Ticks - input.CreatedOn.Ticks).Should().BeLessThan(TimeSpan.TicksPerSecond);
    }


    [TestCase("TSTAlinabaccarat888888", "dbbe3d1028b27f98068b8c1727c2578b")]
    [TestCase("TSTConniebaccarat888888", "9a4fb85060d1f6abd664fc05a4dad1c8")]
    [TestCase("TSTCandicebaccarat888888", "fe726b2413eb7348d2aa8d3357f18a13")]
    [TestCase("TSTHTTW10baccarat10", "f46476128fa66b4a1f19065dc36ac7a5")]
    public void CanMd5HashString(string input, string expected)
    {
        var result = _cryptoProvider.GetMd5HexHash(input);
        Console.WriteLine(result);
        result.Should().Be(expected);
    }


    [TestCase("ZBF1", 4)]
    [TestCase("ZBY2", 4)]
    [TestCase("ZBY2Y", 5)]
    public void CanConvertToBytes(string roomId, int len)
    {
        var bytes = roomId.ToBytesFromString();

        bytes.Length.Should().Be(len);
    }

    [TestCase(
        "Server=host.docker.internal,1433;Database=Lsg;User Id=sa;Password=5VckGdLyvC2zDK8e;TrustServerCertificate=true;"
        , "uX6jIL41nLGk2P6U1xKiqv3tYBhbAKOOlZAKou86yx9Wcym9UJgBsn6uY4g9QFMGsYH1GWSuOBFURUMoPZkk7DNXNEx7ASUOGEXCCoyDUB0iPrXV4czbXYSFdO0HXYENZat2IodX9JjUYdRoayoNT73v")]
    [TestCase("Server=db;Database=Lsg;User Id=sa;Password=5VckGdLyvC2zDK8e;TrustServerCertificate=true;"
        , "hIvucehOYFVw22NYuPUsCFoW9N3HuBn3lZEukFAHyhSfmkj29DihCgt9WmeO8NWTTnVCcIXxHejw7nBLBVzoMmrXtHF2A0dAni1qkapoMXvqRk2HMxlKYhiBtIjzXJf1EA")]
    [TestCase("Server=lsgtestdb;Database=Lsg;User Id=sa;Password=5VckGdLyvC2zDK8e;TrustServerCertificate=true;"
        , "8EO6W7NbxgJvlEJOPBO7BIdD2uslCLPNTyS5bbTOqyWSfZqvfeS4XvIBndK2RGM1KrqfTFw4Pk8EGhUiZw9taMN5fv0chCeW0uDmdoXj6mYD0NPruDQaSJq8rU66ck5uU")]
    public void CanEncryptConnectionString(string input, string expected)
    {
        var result = _cryptoProvider.EncryptBytes(Encoding.UTF8.GetBytes(input));
        Console.WriteLine(result);
        result.Should().Be(expected);
    }
}