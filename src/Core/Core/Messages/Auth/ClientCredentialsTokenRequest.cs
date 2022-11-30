namespace LSG.Core.Messages.Auth
{
    public class ClientCredentialsTokenRequest
    {
        public string client_id { get; set; }

        public string client_secret { get; set; }

        public string grant_type { get; set; }

        public string scope { get; set; }
    }
}