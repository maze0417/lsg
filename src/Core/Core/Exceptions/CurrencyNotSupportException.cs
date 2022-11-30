using System;

namespace LSG.Core.Exceptions
{
    public class CurrencyNotSupportException : Exception
    {
        public CurrencyNotSupportException() : base("Currency is not support.")
        {
        }

        public CurrencyNotSupportException(string currency) : base($"Currency : {currency} is not support")
        {
        }

        public CurrencyNotSupportException(string message, Exception ex) : base(message, ex)
        {
        }
    }
}