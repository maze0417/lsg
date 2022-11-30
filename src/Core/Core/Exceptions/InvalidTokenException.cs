using System;

namespace LSG.Core.Exceptions
{
    public class InvalidTokenException : Exception
    {
        public InvalidTokenException() : base("Invalid Token") { }
        public InvalidTokenException(string message) : base(message) { }
        public InvalidTokenException(string message, Exception ex) : base(message, ex) { }
    }
}