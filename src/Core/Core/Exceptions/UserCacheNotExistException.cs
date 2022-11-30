using System;

namespace LSG.Core.Exceptions
{
    public class UserCacheNotExistException : Exception
    {
        public UserCacheNotExistException() : base("user need login first")
        {
        }


        public UserCacheNotExistException(string message) : base(message)
        {
        }
    }
}