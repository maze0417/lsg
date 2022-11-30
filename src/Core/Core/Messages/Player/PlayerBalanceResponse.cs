using LSG.Core.Messages.Auth;

namespace LSG.Core.Messages.Player
{
    public class PlayerBalanceResponse : UgsResponse
    {
        public decimal bal { get; set; }
        public string cur { get; set; }
        public long balseq { get; set; }
    }
}