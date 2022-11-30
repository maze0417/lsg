namespace LSG.Core.Messages.Auth
{
    public class AuthClientCredentialsResponse : LsgResponse
    {
        public string AccessToken { get; set; }

        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
    }
}