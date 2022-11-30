using System;
using System.Linq;
using LSG.SharedKernel.AppConfig;
using Microsoft.Extensions.Configuration;

namespace LSG.SharedKernel.Elk
{
    public interface IElkConfig
    {
        Uri[] Urls { get; }
    }

    public sealed class ElkConfig : BaseAppConfig, IElkConfig
    {
        public ElkConfig(IConfiguration configuration) : base(configuration)
        {
        }

        Uri[] IElkConfig.Urls => Config.GetSection("Elk:Urls").Get<string[]>().Select(a => new Uri(a)).ToArray();
    }
}