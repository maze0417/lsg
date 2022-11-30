using System;

namespace LSG.Core.Exceptions
{
    public class ExpiredLiveClientException : Exception
    {
        public ExpiredLiveClientException() : base("Live client expired not exist in manager")
        {
        }

        public ExpiredLiveClientException(string key) : base(
            $"Live client expired not exist in manager for {key}")
        {
        }
    }
}