using System;

namespace LSG.Core.Exceptions
{
    public class MissingTokenException : Exception
    {
        public MissingTokenException() : base("Missing Token")
        {
        }

        public MissingTokenException(string message) : base(message)
        {
        }

        public MissingTokenException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}