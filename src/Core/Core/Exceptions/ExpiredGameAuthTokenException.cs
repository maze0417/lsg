using System;

namespace LSG.Core.Exceptions
{
    public class ExpiredLiveStreamException : Exception
    {
        public ExpiredLiveStreamException() : base("Expired Game AuthToken ")
        {
        }

        public ExpiredLiveStreamException(string message) : base(message)
        {
        }

        public ExpiredLiveStreamException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}