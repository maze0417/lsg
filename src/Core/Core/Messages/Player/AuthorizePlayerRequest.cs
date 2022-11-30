using LSG.Core.Enums;

namespace LSG.Core.Messages.Player
{
    public class AuthorizePlayerRequest
    {
        public string ipaddress { get; set; }
        public string username { get; set; }
        public string userid { get; set; }
        public string lang { get; set; }
        public string cur { get; set; }
        public int betlimitid { get; set; }
        public string loginurl { get; set; }
        public string cashierurl { get; set; }
        public string helpurl { get; set; }
        public string termsurl { get; set; }
        public PlatformType? platformtype { get; set; }
        public bool istestplayer { get; set; }
        public PlayerType playertype { get; set; }
    }
}