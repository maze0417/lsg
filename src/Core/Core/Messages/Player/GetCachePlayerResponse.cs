using System;

namespace LSG.Core.Messages.Player
{
    public sealed class GetCachePlayerResponse : LsgResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ExternalId { get; set; }

        public Guid? OnlineAnchorId { get; set; }
        public string OnlineAnchorName { get; set; }
        public bool CanLike { get; set; }

        public string LiveStreamToken { get; set; }
        public string Pid { get; set; }
    }
}