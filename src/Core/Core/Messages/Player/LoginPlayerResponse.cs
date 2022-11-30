namespace LSG.Core.Messages.Player
{
    public sealed class LoginPlayerResponse : LsgResponse
    {
        public string Token { get; set; }
    }
}