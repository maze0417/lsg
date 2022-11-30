using System;

namespace LSG.Infrastructure.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RouteAcceptWhenSiteIsAttribute : Attribute
    {
        public RouteAcceptWhenSiteIsAttribute(params string[] sites)
        {
            Sites = sites;
        }

        public string[] Sites { get; }
    }
}