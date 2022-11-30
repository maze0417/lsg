using System;

namespace LSG.Core.Exceptions
{
    public class ModelBindingException : Exception
    {
        public ModelBindingException(string message) : base(message)
        {
        }
    }
}