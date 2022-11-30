using System;

namespace LSG.Core.Messages.Events
{
    public abstract class BaseEvent
    {
        public Guid CorrelationId { get; set; }
    }
}