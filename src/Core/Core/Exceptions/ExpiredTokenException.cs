using System;

namespace LSG.Core.Exceptions
{
    public class ExpiredTokenException : Exception
    {
        public ExpiredTokenException() : base("Expired token")
        {
        }

        public ExpiredTokenException(string message) : base(message)
        {
        }
    }
}