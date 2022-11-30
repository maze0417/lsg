using System;
using System.Security.Cryptography;
using System.Text;
using LSG.SharedKernel.Extensions;

namespace LSG.Infrastructure.Security
{
    public interface ICryptoProvider
    {
        string EncryptBytes(byte[] bytes);
        byte[] DecryptBytes(string plainText);

        string GetMd5HexHash(string input);
        string GetSha512HashToBase64(string input);

        string EncryptString(string input);
        string DecryptString(string input);
    }

    public sealed class CryptoProvider : ICryptoProvider
    {
        private readonly IDataEncoder _encoder;

        private readonly byte[] _encryptKey =
            "EF083E6D901B4C72A1EC0DD3BD6DB271"
                .ToBytesFromHexString();

        private readonly SymmetricAlgorithm _cryptoService = Aes.Create();
        private readonly ICryptoProvider _this;


        public CryptoProvider(IDataEncoder encoder)
        {
            _encoder = encoder;
            _this = this;
        }

        string ICryptoProvider.EncryptBytes(byte[] bytes)
        {
            var encryptedBytes = _cryptoService.CreateEncryptor(_encryptKey, _encryptKey)
                .TransformFinalBlock(bytes, 0, bytes.Length);

            return _encoder.Encode(encryptedBytes);
        }

        byte[] ICryptoProvider.DecryptBytes(string plainText)
        {
            var bytes = _encoder.Decode(plainText);
            return _cryptoService.CreateDecryptor(_encryptKey, _encryptKey).TransformFinalBlock(bytes, 0, bytes.Length);
        }

        string ICryptoProvider.GetMd5HexHash(string input)
        {
            using var md5Hash = MD5.Create();

            var data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            return data.ToHexString();
        }

        string ICryptoProvider.GetSha512HashToBase64(string input)
        {
            var sha512 = SHA512.Create();
            return Convert.ToBase64String(sha512.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        string ICryptoProvider.EncryptString(string input)
        {
            return _this.EncryptBytes(Encoding.UTF8.GetBytes(input));
        }

        string ICryptoProvider.DecryptString(string input)
        {
            return Encoding.UTF8.GetString(_this.DecryptBytes(input));
        }
    }
}