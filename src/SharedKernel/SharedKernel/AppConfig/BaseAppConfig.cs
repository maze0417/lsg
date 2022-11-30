using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using LSG.SharedKernel.Extensions;
using Microsoft.Extensions.Configuration;

namespace LSG.SharedKernel.AppConfig
{
    public abstract class BaseAppConfig
    {
        protected static IConfiguration Config;

        private static readonly IDictionary<Type, TypeConverterAttribute> TypeConverterAttributesMapper =
            new Dictionary<Type, TypeConverterAttribute>
            {
                {typeof(bool), new TypeConverterAttribute(typeof(BooleanConverterFromInt))},
            };

        protected BaseAppConfig(IConfiguration configuration)
        {
            Config ??= configuration;
            TypeConverterAttributesMapper.ForEach(s =>
            {
                var (key, value) = s;
                TypeDescriptor.AddAttributes(key, value);
            });
        }


        protected static T Bind<T>(string sectionName) where T : new()
        {
            var section = Config?.GetSection(sectionName);
            if (section == null || !section.Exists()) return default;


            return section.Get<T>();
        }

        protected static T Bind<T>(string sectionName, T val) where T : new()
        {
            var section = Config?.GetSection(sectionName);
            if (section == null || !section.Exists()) return default;

            section.Bind(val);
            return val;
        }


        protected static T Get<T>(string key, T defaultValue = default, Func<string, T> converter = null)
        {
            if (converter == null) converter = v => (T) Convert.ChangeType(v, typeof(T));
            var value = Config[key];
            return value == null ? defaultValue : converter(value);
        }

        protected static Dictionary<string, string> GetAllSetting()
        {
            return Config.AsEnumerable().ToDictionary(a => a.Key, a => a.Value);
        }


        public static IConfiguration GetConfiguration()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            Config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile(
                    $"appsettings.{environment}.json",
                    optional: true,
                    reloadOnChange: true)
                .Build();
            return Config;
        }
    }
}