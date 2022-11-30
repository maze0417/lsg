using System.Linq;
using FluentAssertions;
using LSG.Core;
using LSG.Hosts.LsgApi;
using LSG.Infrastructure;
using LSG.Infrastructure.Security;
using LSG.SharedKernel.AppConfig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.IntegrationTests.SharedKernel;

[TestFixture]
[Category(Const.TestCategory.AppConfig)]
public class AppConfigTests : Base<LsgApiStartup>
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void CanParseForTestSection()
    {
        var config = new TestAppConfig(BaseAppConfig.GetConfiguration());
        var test = config.GetForTest();
        test.TestBool.Should().BeTrue();
        test.TestName.Should().Be("myname");
        test.TestValue.Should().Be("myvalue");
        test.TestInt.Should().Be(901);
        test.TestBool2.Should().Be(true);
        test.TestArray.First().Should().Be("t1");
    }

    [Test]
    public void ConfigurationInstanceShouldBeTheSame()
    {
        var config = BaseAppConfig.GetConfiguration();
        var hashA = new TestAppConfig(config).ExposedConfig.GetHashCode();
        var hashB = new TestAppConfig(config).ExposedConfig.GetHashCode();
        var hashC = new Test2Config(config).ExposedConfig.GetHashCode();

        hashA.Should().Be(hashB);
        hashA.Should().Be(hashC);
    }

    [Test]
    public void CanParseLoggingSection()
    {
        var config = new TestAppConfig(BaseAppConfig.GetConfiguration());
        var test = config.GetLogging();
        test.MinimumLevel.Default.Should().Be("Debug");
    }

    [Test]
    public void CanGetProperty()
    {
        var config = new TestAppConfig(BaseAppConfig.GetConfiguration());
        config.TestKey.Should().Be("testvalue");
    }


    [Test]
    public void CanGetCdnUrl()
    {
        var config = DefaultFactory.GetRequiredService<ILsgConfig>();
        config.CdnPath.Should().Be("https://gci.bsdapi.com/ZB/Pc/resource/PCPlaza/assets/anchors/");
    }

    [Test]
    public void CanGetUgsConfig()
    {
        var config = DefaultFactory.GetRequiredService<ILsgConfig>();

        config.UgsConfig.UgsApiUrl.Should().Be("http://ugslicenseeoperatorapi");
        
    }

    [Test]
    public void CanGetAdminConfig()
    {
        var config = DefaultFactory.GetRequiredService<ILsgConfig>();

        var cry = DefaultFactory.GetRequiredService<ICryptoProvider>();
        var pwd = config.AdminConfig["admin"];
        pwd.Should().NotBeNullOrEmpty();
        cry.DecryptString(pwd).Should().Be("adminP@ssw0rd");
    }

    private class TestAppConfig : BaseAppConfig
    {
        public TestAppConfig(IConfiguration configuration) : base(configuration)
        {
        }

        public IConfiguration ExposedConfig => Config;
        public string TestKey => Get("TestKey", string.Empty);

        public Logging GetLogging()
        {
            return Bind<Logging>("Serilog");
        }

        public ForTest GetForTest()
        {
            return Bind<ForTest>("ForTest");
        }

        public class Logging
        {
            public LogLevel MinimumLevel { get; set; }
        }

        public class LogLevel
        {
            public string Default { get; set; }
        }

        public class ForTest
        {
            public string TestName { get; set; }
            public string TestValue { get; set; }
            public int TestInt { get; set; }
            public bool TestBool { get; set; }
            public bool TestBool2 { get; set; }
            public string[] TestArray { get; set; }
        }
    }

    public class Test2Config : BaseAppConfig
    {
        public Test2Config(IConfiguration configuration) : base(configuration)
        {
        }

        public IConfiguration ExposedConfig => Config;
    }
}