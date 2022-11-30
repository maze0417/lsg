namespace LSG.Core.Messages.Auth
{
    public class UgsResponse
    {
        public int err { get; set; }
        public string errdesc { get; set; }
    }

    public class TokenResponse
    {
        public string access_token { get; set; }

        public string token_type { get; set; }

        public int expires_in { get; set; }

        public string refresh_token { get; set; }

        public string scope { get; set; }

        public string error { get; set; }

        public string error_description { get; set; }
    }
}