using System;
using System.Linq;
using System.Text;

namespace LSG.SharedKernel.Extensions
{
    public static class BytesExtensions
    {
        public static byte[] ConcatenateBytes(this byte[] a, byte[] b)
        {
            var r = new byte[a.Length + b.Length];

            Buffer.BlockCopy(a, 0, r, 0, a.Length);
            Buffer.BlockCopy(b, 0, r, a.Length, b.Length);

            return r;
        }

        public static string ToHexStringWithSplit(this byte[] data)
        {
            return string.Join(' ', Split(data.ToHexString(), 4));

            static string[] Split(string str, int chunkSize)
            {
                return Enumerable.Range(0, str.Length / chunkSize)
                    .Select(i => str.Substring(i * chunkSize, chunkSize)).ToArray();
            }
        }

        public static string ToHexString(this byte[] data)
        {
            var sBuilder = new StringBuilder();

            foreach (var b in data)
            {
                sBuilder.Append(b.ToString("x2"));
            }

            return sBuilder.ToString();
        }
    }
}