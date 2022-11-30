using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace LSG.SharedKernel.Extensions
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static bool IsNotNullOrEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }

        public static string NoLongerThan(this string str, int chars)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= chars)
                return str;
            return str.Substring(0, chars);
        }

        public static bool IgnoreCaseEquals(this string str, string second)
        {
            return string.Equals(str, second, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IgnoreCaseNotEquals(this string str, string second)
        {
            return !string.Equals(str, second, StringComparison.OrdinalIgnoreCase);
        }

        public static T Deserialize<T>(this string str)
        {
            return JsonSerializer.Deserialize<T>(str);
        }

        public static byte[] ToBytesFromHexString(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                .ToArray();
        }

        public static string ToUrl(this string url, string path, string query = "")
        {
            return new UriBuilder(url)
            {
                Path = path,
                Query = query
            }.Uri.AbsoluteUri;
        }

        public static string JoinAsStringByComma<T>(this IEnumerable<T> enumerable)
        {
            return string.Join(",", enumerable);
        }

        public static string JoinAsString<T>(this IEnumerable<T> enumerable, string separator)
        {
            return string.Join(separator, enumerable);
        }

        public static byte[] ToBytesFromString(this string text, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            return encoding.GetBytes(text);
        }

        public static string WithDbLength(this string columnType, int length)
        {
            return $"{columnType}({length})";
        }

        public static string ReplaceDoubleQuotationMarkBySingleQuotationMark(this string str)
        {
            return str.Replace("\"\"", "\"");
        }

        public static string ReplaceSpaceByUnderscore(this string str)
        {
            return str.Replace(" ", "_");
        }

        public static string ReplaceApostropheByEmpty(this string str)
        {
            return str.Replace("'", string.Empty);
        }

        public static string[] SplitBySemicolon(this string s)
        {
            return string.IsNullOrEmpty(s) ? new string[0] : s.Split(';');
        }

        public static string[] SplitByColon(this string s)
        {
            return s.Split(':');
        }

        public static string[] SplitByComma(this string s)
        {
            return s.Split(',');
        }

        public static string[] SplitByUnderscore(this string s)
        {
            return s.Split('_');
        }

        public static string CamelCaseToWords(this string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }

            var sb = new StringBuilder();
            char? prevChar = null;
            foreach (var curr in str)
            {
                if (prevChar.HasValue)
                {
                    var prev = prevChar.Value;

                    if (char.IsLower(prev) && char.IsUpper(curr))
                    {
                        sb.Append(' ');
                    }
                }

                sb.Append(curr);
                prevChar = curr;
            }

            return sb.ToString();
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}