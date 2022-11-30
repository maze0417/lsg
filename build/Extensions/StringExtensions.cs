using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Builds.Deployment.Enums;

namespace Builds.Deployment.Extensions
{
    public static class StringExtensions
    {
        public static (bool, TEnum) Parse<TEnum>(this string source) where TEnum : struct, IConvertible
        {
            if (Enum.TryParse(source, true, out TEnum env))
            {
                return (true, env);
            }

            var fieldInfo = typeof(EnvironmentType).GetFields().FirstOrDefault(a =>
                a.GetCustomAttribute<DescriptionAttribute>()
                    ?.Description.Equals(source, StringComparison.OrdinalIgnoreCase) == true);

            // ReSharper disable once PossibleNullReferenceException
            return fieldInfo == null ? default : (true, (TEnum) fieldInfo.GetValue(null));
        }

        public static bool ShouldNotNullOrEmpty(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }
    }
}