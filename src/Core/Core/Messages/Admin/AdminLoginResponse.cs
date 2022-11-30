namespace LSG.Core.Messages.Admin
{
    public sealed class AdminLoginResponse : LsgResponse
    {
        public string Token { get; set; }
    }
}