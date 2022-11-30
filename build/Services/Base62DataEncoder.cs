using System;
using System.Collections.Generic;
using System.Text;

namespace Builds.Deployment.Services
{
    public interface IDataEncoder
    {
        string Encode(byte[] bytes);
        byte[] Decode(string encoded);
    }

    public sealed class Base62DataEncoder : IDataEncoder
    {
        const string DefaultCharacterSet = "GjUKez74PgQLhO3iopxsk62HYSTWVanDCR9BEFZ1MJcby0muXAvlNdw5ftrIq8";

        string IDataEncoder.Encode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Empty value passed to be encoded");
            }

            var converted = BaseConvert(bytes, 256, 62);
            var builder = new StringBuilder();
            foreach (var t in converted)
            {
                builder.Append(DefaultCharacterSet[t]);
            }

            return builder.ToString();
        }

        byte[] IDataEncoder.Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException("Empty value passed to be decoded");
            }

            var arr = new byte[value.Length];
            for (var i = 0; i < arr.Length; i++)
            {
                arr[i] = (byte) DefaultCharacterSet.IndexOf(value[i]);
            }

            return BaseConvert(arr, 62, 256);
        }


        static byte[] BaseConvert(byte[] source, int sourceBase, int targetBase)
        {
            var result = new List<int>();
            int count;
            while ((count = source.Length) > 0)
            {
                var quotient = new List<byte>();
                int remainder = 0;
                for (var i = 0; i != count; i++)
                {
                    int accumulator = source[i] + remainder * sourceBase;
                    byte digit = Convert.ToByte((accumulator - (accumulator % targetBase)) / targetBase);
                    remainder = accumulator % targetBase;
                    if (quotient.Count > 0 || digit != 0)
                    {
                        quotient.Add(digit);
                    }
                }

                result.Insert(0, remainder);
                source = quotient.ToArray();
            }

            var output = new byte[result.Count];
            for (var i = 0; i < result.Count; i++)
                output[i] = (byte) result[i];

            return output;
        }
    }
}