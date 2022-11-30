using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Builds.Deployment.Services
{
    public sealed class EncryptProvider
    {
        static readonly IDataEncoder Encoder = new Base62DataEncoder();

        const string Salt = "BD6DB271E2A1EC0DD3F083E6D901B4C7";

        static readonly byte[] EncryptKey =
            Enumerable.Range(0, Salt.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(Salt.Substring(x, 2), 16))
                .ToArray();


        static readonly SymmetricAlgorithm CryptoService = Aes.Create();


        public static string Encrypt(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var encryptedBytes = CryptoService.CreateEncryptor(EncryptKey, EncryptKey)
                .TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoder.Encode(encryptedBytes);
        }

        public static string Decrypt(string plainText)
        {
            var bytes = Encoder.Decode(plainText);
            var result = CryptoService.CreateDecryptor(EncryptKey, EncryptKey)
                .TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(result);
        }
    }
}