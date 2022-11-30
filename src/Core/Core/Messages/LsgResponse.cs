using LSG.Core.Enums;

namespace LSG.Core.Messages
{
    public class LsgResponse
    {
        public ApiResponseCode Code { get; set; }
        public string Message { get; set; }
    }
}