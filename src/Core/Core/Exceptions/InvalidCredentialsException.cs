using System;

namespace LSG.Core.Exceptions
{
    public class InvalidCredentialsException : Exception
    {
        public InvalidCredentialsException() : base("Invalid Credentials")
        {
        }

        public InvalidCredentialsException(string message) : base($"Invalid Credentials: {message}")
        {
        }
    }
}