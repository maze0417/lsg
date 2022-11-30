using System;
using System.Text;

namespace LSG.SharedKernel.Extensions
{
    public static class ExceptionExtensions
    {
        public static string GetMessageChain(this Exception ex)
        {
            if (ex == null)
                return null;
            if (ex.InnerException == null)
                return ex.Message;
            int num = 0;
            var stringBuilder = new StringBuilder();
            while (ex != null)
            {
                if (num > 0)
                    stringBuilder.Append("; INNER ").Append(num).Append(':').Append(' ');
                stringBuilder.Append(ex.Message);
                ex = ex.InnerException;
                ++num;
            }

            return stringBuilder.ToString();
        }
    }
}