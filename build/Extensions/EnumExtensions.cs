using System;
using System.ComponentModel;
using System.Reflection;

namespace Builds.Deployment.Extensions
{
    public static class EnumExtensions
    {
        public static string Description(this Enum value)
        {
            var desc = value
                .GetType()
                .GetField(value.ToString())
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description;
            return desc ?? value.ToString();
        }
    }
}