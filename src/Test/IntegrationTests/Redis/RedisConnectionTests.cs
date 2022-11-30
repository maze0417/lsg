using System;
using FluentAssertions;
using LSG.Hosts.LsgApi;
using LSG.SharedKernel.Redis;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.IntegrationTests.Redis
{
    [TestFixture]
    public class RedisConnectionTests : Base<LsgApiStartup>
    {
        [Test]
        public void CanGetConfig()
        {
            var core = DefaultFactory.GetService<IRedisConfig>();
            core.RedisConnectionString.Should().NotBeNullOrEmpty();
            core.RedisConnectionString.Contains("core").Should().BeTrue();
            core.SignalRRedisConnectionString.Contains("signalr").Should().BeTrue();
        }

        [Test]
        public void CanGetDifferentRedisConnection()
        {
            var connectionFactory = DefaultFactory.GetService<Func<Type, IRedisConnection>>();

            var core = connectionFactory(typeof(RedisConnection));
            core.Should().BeAssignableTo<RedisConnection>();
            var signal = connectionFactory(typeof(SignalRedisConnection));
            signal.Should().BeAssignableTo<SignalRedisConnection>();
        }
    }
}