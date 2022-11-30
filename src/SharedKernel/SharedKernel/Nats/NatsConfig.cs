using LSG.SharedKernel.AppConfig;
using Microsoft.Extensions.Configuration;

namespace LSG.SharedKernel.Nats
{
    public interface INatsConfig
    {
        string[] Urls { get; }
    }

    public sealed class NatsConfig : BaseAppConfig, INatsConfig
    {
        public NatsConfig(IConfiguration configuration) : base(configuration)
        {
        }

        string[] INatsConfig.Urls => Config.GetSection("Nats:Urls").Get<string[]>();
    }
}