using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace LSG.SharedKernel.Extensions
{
    public static class DictionaryExtensions
    {
        public static string ToDetailString(this IDictionary<string, StringValues> header)
        {
            if (header == null)
                return string.Empty;
            var value = header.Keys
                .Select(key => $"{key}: {header[key]}")
                .ToArray();

            return value.Length > 0 ? string.Join("\r\n", value) : string.Empty;
        }

        public static string ToDetailString(this IDictionary<string, object> dictionary, string symbol = "=")
        {
            if (dictionary == null)
                return string.Empty;
            var value = dictionary.Keys
                .Select(key => $"{key} {symbol} {dictionary[key]}")
                .ToArray();

            return value.Length > 0 ? string.Join("\r\n", value) : string.Empty;
        }
    }
}