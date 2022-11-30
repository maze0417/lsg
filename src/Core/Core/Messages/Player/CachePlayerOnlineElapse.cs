using System;

namespace LSG.Core.Messages.Player
{
    public class CachePlayerOnlineElapse
    {
        public Guid PlayerId { get; set; }

        /// <summary>
        /// 當日累計上線秒數
        /// </summary>
        public int DailyOnlineSeconds { get; set; }

        /// <summary>
        /// 最後更新上線時間
        /// </summary>
        public DateTimeOffset LastElapsedTime { get; set; }

        /// <summary>
        /// 最後異動的秒數
        /// </summary>
        public int LastOffsetSeconds { get; set; }

        /// <summary>
        /// 當日大聲公累計上線秒數 (>=30 分鐘可用)
        /// </summary>
        public int DailyMegaphoneOnlineSeconds { get; set; }
    }
}