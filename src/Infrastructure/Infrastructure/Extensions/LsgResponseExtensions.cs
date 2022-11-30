using System;
using LSG.Core.Enums;
using LSG.Core.Messages;

namespace LSG.Infrastructure.Extensions
{
    public static class LsgResponseExtensions
    {
        public static void ThrowIfNotSuccess(this LsgResponse response)
        {
            if (response.Code != ApiResponseCode.Success)
            {
                throw new Exception($"api error {response.Code} ,message {response.Message}");
            }
        }
    }
}