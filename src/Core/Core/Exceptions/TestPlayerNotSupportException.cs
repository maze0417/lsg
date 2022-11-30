using System;

namespace LSG.Core.Exceptions
{
    public class TestPlayerNotSupportException : Exception
    {
        public TestPlayerNotSupportException() : base("Test player is not supported.")
        {
        }

        public TestPlayerNotSupportException(string message) : base(message)
        {
        }

        public TestPlayerNotSupportException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}