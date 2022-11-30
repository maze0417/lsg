using System;

namespace LSG.SharedKernel.Logger
{
    public class LogEventWrapper
    {
        public DateTimeOffset Timestamp { get; set; }

        public string MessageTemplate { get; set; }

        public string LogEventString { get; set; }
    }
}