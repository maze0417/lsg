namespace LSG.Core.Messages.Player
{
    public class AuthorizePlayerResponse
    {
        public int err { get; set; }

        public string errdesc { get; set; }

        public string authtoken { get; set; }

        public bool isnew { get; set; }
    }
}