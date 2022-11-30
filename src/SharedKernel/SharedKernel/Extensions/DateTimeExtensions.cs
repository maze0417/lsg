using System;

namespace LSG.SharedKernel.Extensions
{
    public static class DateTimeExtensions
    {
        private const long UnixEpoch = 621355968000000000L;

        public static long ToUnixTimeSeconds(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime().Ticks - UnixEpoch) / TimeSpan.TicksPerSecond;
        }

        public static DateTime UnixTimeToUtcDateTime(this long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            var dtDateTime = new DateTime(UnixEpoch, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTimeOffset UnixTimeToUtcDateTimeOffset(this long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp);
        }

        public static DateTimeOffset UnixTimeToAsiaDateTimeOffset(this long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).ToOffset(TimeSpan.FromHours(8));
        }

        public static DateTimeOffset UnixTimeToEastUsDateTimeOffset(this long unixTimeStamp)
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTimeStamp).ToOffset(TimeSpan.FromHours(-4));
        }

        public static DateTime DateTimeOffsetToAsiaDateTime(this DateTimeOffset dt)
        {
            return dt.UtcDateTime.AddHours(8);
        }

        public static string ToNormal(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss");
        }
    }
}