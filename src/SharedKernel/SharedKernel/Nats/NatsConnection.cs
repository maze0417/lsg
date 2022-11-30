using LSG.SharedKernel.Logger;
using NATS.Client;

namespace LSG.SharedKernel.Nats
{
    public interface INatsConnection
    {
        IConnection Connection { get; }
        
        INatsConfig Config { get; }
    }

    public sealed class NatsConnection : BaseConnectionFactory, INatsConnection
    {
        public NatsConnection(INatsConfig natsConfig, ILsgLogger lsgLogger) : base(natsConfig, lsgLogger)
        {
            Config = natsConfig;
        }

        public IConnection Connection => LazyConnection.Value;
        public INatsConfig Config { get; }
    }
}