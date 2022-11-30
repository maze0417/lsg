using System;

namespace LSG.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class SuppressHttpCallLogAttribute : Attribute
    {
    }
}