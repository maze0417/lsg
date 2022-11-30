using System;

namespace LSG.Core.Exceptions
{
    public class OperationNotAllowedException : Exception
    {
        public OperationNotAllowedException(string message) : base(message)
        {
        }
    }
}