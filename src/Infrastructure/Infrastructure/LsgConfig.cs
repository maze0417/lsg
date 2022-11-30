using System;
using System.Collections.Generic;
using LSG.Core;
using LSG.Core.Messages;
using LSG.SharedKernel.AppConfig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LSG.Infrastructure;

public interface ILsgConfig
{
    string LsgConnectionString { get; }
    string LsgReadOnlyConnectionString { get; }


    Uri LsgApiUrl { get; }

    Uri LsgFrontendUrl { get; }

    Uri LoggerWorkerURl { get; }


    IHostEnvironment Environment { get; }


    string CurrentSite { get; }


    string CdnPath { get; }


    string LsgFrontendUIStyle { get; }


    Dictionary<string, string> AdminConfig { get; }

    UgsConfig UgsConfig { get; }
    IDictionary<string, string> GetAllSetting();
}

public sealed class LsgConfig : BaseAppConfig, ILsgConfig
{
    private readonly IHostEnvironment _environment;
    private readonly string _site;

    public LsgConfig(string site, IConfiguration configuration, IHostEnvironment environment) : base(configuration)
    {
        _environment = environment;
        _site = site;
    }


    string ILsgConfig.LsgConnectionString => Config.GetConnectionString(Const.ConnectionStringNames.Lsg);

    public string LsgReadOnlyConnectionString =>
        Config.GetConnectionString(Const.ConnectionStringNames.LsgReadOnly);

    Uri ILsgConfig.LsgApiUrl => Get("LsgApiUrl", default, s => new Uri(s));

    Uri ILsgConfig.LsgFrontendUrl => Get("LsgFrontendUrl", default, s => new Uri(s));

    Uri ILsgConfig.LoggerWorkerURl => Get("LoggerWorkerURl", default, s => new Uri(s));

    UgsConfig ILsgConfig.UgsConfig => Bind<UgsConfig>(Const.ConfigSectionName.UgsConfig);

    IHostEnvironment ILsgConfig.Environment => _environment;


    IDictionary<string, string> ILsgConfig.GetAllSetting()
    {
        return GetAllSetting();
    }


    string ILsgConfig.CurrentSite => _site;

    string ILsgConfig.CdnPath =>
        Get("CdnPath", string.Empty);

    string ILsgConfig.LsgFrontendUIStyle => Get("LsgFrontendUIStyle", "1");

    Dictionary<string, string> ILsgConfig.AdminConfig =>
        Bind(Const.ConfigSectionName.AdminConfig,
            new Dictionary<string, string>());
}