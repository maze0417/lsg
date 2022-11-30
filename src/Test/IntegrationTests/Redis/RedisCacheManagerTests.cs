using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core.Messages.Player;
using LSG.Hosts.LsgApi;
using LSG.SharedKernel.Logger;
using LSG.SharedKernel.Redis;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace LSG.IntegrationTests.Redis
{
    [TestFixture]
    public class RedisCacheManagerTests : Base<LsgApiStartup>
    {
        private readonly Guid _cacheKey = new Guid("D0EB25B4-0EE6-4BC6-A793-1483FF3DFA74");

        [TestCase(typeof(RedisConnection))]
        [TestCase(typeof(SignalRedisConnection))]
        public async Task CanSafelyGetAndCacheDataWhenNoException(Type type)
        {
            const string newData = "Newdata";
            const string cacheType = "DataByGuid";
            var testQueries = Substitute.For<ITestQueries>();
            testQueries.GetDataAsync().Returns(Task.FromResult(newData));

            var cacheManagerFactory = DefaultFactory.GetService<Func<Type, IRedisCacheManager>>();

            await cacheManagerFactory(type).ClearCacheAsync(cacheType, _cacheKey);

            var data = await cacheManagerFactory(type)
                .SafeStringGetAndCacheAsync(cacheType, _cacheKey, () => testQueries.GetDataAsync());

            data.Should().Be(newData);
            testQueries.Received(1);

            var cacheData = await cacheManagerFactory(type)
                .SafeStringGetAsync(cacheType, _cacheKey, () => testQueries.GetDataAsync());
            data.Should().Be(cacheData);
        }


        [Test]
        public async Task CanSafelyGetAndCacheDataWhenException()
        {
            const string newData = "Newdata1";
            const string cacheType = "DataByGuid1";
            var testQueries = Substitute.For<ITestQueries>();
            testQueries.GetDataAsync().Returns(Task.FromResult(newData));

            IRedisConnection connection = new RedisConnection(DefaultFactory.GetService<IRedisConfig>(),
                DefaultFactory.GetService<ILsgLogger>());
            connection.Connection.Dispose();

            IRedisCacheManager cacheManager =
                new RedisCacheManager(connection, DefaultFactory.GetService<ILsgLogger>());


            var data = await cacheManager.SafeStringGetAndCacheAsync(cacheType, _cacheKey,
                () => testQueries.GetDataAsync());

            data.Should().Be(newData);
            testQueries.Received(1);

            var con = DefaultFactory.GetService<IRedisConnection>();
            var value = con.Connection.GetDatabase().StringGet(cacheType);
            value.HasValue.Should().BeFalse();
        }

        [Test]
        public async Task CanSafelyGetDataWhenException()
        {
            const string newData = "Newdata1";
            const string cacheType = "DataByGuid1";
            var testQueries = Substitute.For<ITestQueries>();
            testQueries.GetDataAsync().Returns(Task.FromResult(newData));

            IRedisConnection connection = new RedisConnection(DefaultFactory.GetService<IRedisConfig>(),
                DefaultFactory.GetService<ILsgLogger>());

            IRedisCacheManager cacheManager =
                new RedisCacheManager(connection, DefaultFactory.GetService<ILsgLogger>());


            await cacheManager.ClearCacheAsync(cacheType, _cacheKey);
            var data = await cacheManager.SafeStringGetAsync(cacheType, _cacheKey,
                () => testQueries.GetDataAsync());

            data.Should().Be(newData);
            testQueries.Received(1);

            var con = DefaultFactory.GetService<IRedisConnection>();
            var value = con.Connection.GetDatabase().StringGet(cacheType);
            value.HasValue.Should().BeFalse();
        }

        [TestCase(typeof(RedisConnection))]
        [TestCase(typeof(SignalRedisConnection))]
        public async Task CanSafelyCacheData(Type type)
        {
            const string newData = "Newdata2";
            const string cacheType = "DataByGuid2";
            var testQueries = Substitute.For<ITestQueries>();
            testQueries.GetDataAsync().Returns(Task.FromResult(newData));

            var cacheManagerFactory = DefaultFactory.GetService<Func<Type, IRedisCacheManager>>();

            await cacheManagerFactory(type).ClearCacheAsync(cacheType, _cacheKey);


            await cacheManagerFactory(type).SafeStringCacheAsync(cacheType, _cacheKey, newData);

            var data = await cacheManagerFactory(type)
                .SafeStringGetAsync(cacheType, _cacheKey, () => testQueries.GetDataAsync());

            data.Should().Be(newData);
            testQueries.Received(0);
        }

        [TestCase(typeof(RedisConnection))]
        [TestCase(typeof(SignalRedisConnection))]
        public async Task CanCacheAndClearData(Type type)
        {
            const string newData = "Newdata3";
            const string cacheType = "DataByGuid3";
            var testQueries = Substitute.For<ITestQueries>();
            testQueries.GetDataAsync().Returns(Task.FromResult(newData));

            var cacheManagerFactory = DefaultFactory.GetService<Func<Type, IRedisCacheManager>>();
            await cacheManagerFactory(type).SafeStringCacheAsync(cacheType, _cacheKey, newData);

            await cacheManagerFactory(type).ClearCacheAsync(cacheType, _cacheKey);


            var data = await cacheManagerFactory(type).StringGetAsync<string>(cacheType, _cacheKey);

            data.Should().BeNullOrEmpty();
        }


        [TestCase(2)]
        public async Task CanCount(int start)
        {
            for (int i = 0; i < 10; i++)
            {
                const string newData = "IncrementAsync";
                const string cacheType = "Count1";
                var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

                var s = await cacheManager.StringGetAsync<int>(cacheType, newData);

                Console.WriteLine($@"original {s}");
                var res = await cacheManager.IncrementAsync(cacheType, newData, start);
                Console.WriteLine($@"{s}+{start} = {res}");

                res.Should().Be(start + s);
            }
        }

        [TestCase(typeof(RedisConnection))]
        [TestCase(typeof(SignalRedisConnection))]
        public async Task CanUpdateCache(Type type)
        {
            const string newData = "Newdata4";
            const string cacheType = "DataByGuid4";

            var cacheManagerFactory = DefaultFactory.GetService<Func<Type, IRedisCacheManager>>();
            await cacheManagerFactory(type).SafeStringCacheAsync(cacheType, _cacheKey, newData);

            var data = await cacheManagerFactory(type).StringGetAsync<string>(cacheType, _cacheKey);

            data.Should().Be(newData);

            const string updateData = "updated";
            await cacheManagerFactory(type).SafeStringCacheAsync(cacheType, _cacheKey, updateData);

            var update = await cacheManagerFactory(type).StringGetAsync<string>(cacheType, _cacheKey);

            update.Should().Be(updateData);
        }


        [Test]
        public async Task CanHashSetObjectAndGet()
        {
            const string cacheType = "TestHashSet";

            var key = "d041038b-dedb-4ba5-bdb0-db7f44c37315";

            var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

            var data = new CachePlayer
            {
                Id = new Guid(key),
                Level = 123,
                Name = "test",
                BrandId = Guid.NewGuid(),
                CultureCode = "zh-cn",
                ExternalId = "Testuser",
                DefaultWalletId = Guid.NewGuid(),
                DefaultWalletCurrencyCode = "cny",
           
            };
            await cacheManager.HashSetAsync(cacheType, key, data);


            var redisData = await cacheManager.HashGetAsync<CachePlayer>(cacheType, key);


            redisData.Should().BeEquivalentTo(data);
        }

        [Test]
        public async Task CanHashSetObjectAndGetWithDifferentObject()
        {
            const string cacheType = "TestHashSet2";

            var key = "2041038b-dedb-4ba5-bdb0-db7f44c37312";

            var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

            var data = new CachePlayer
            {
                Id = new Guid(key),
                Level = 123,
                Name = "test",
                BrandId = Guid.NewGuid(),
                CultureCode = "zh-cn",
                ExternalId = "Testuser",
                DefaultWalletId = Guid.NewGuid(),
                DefaultWalletCurrencyCode = "cny"
            };
            await cacheManager.HashSetAsync(cacheType, key, data);


            var redisData = await cacheManager.HashGetAsync<CachePlayer>(cacheType, key);


            redisData.Should().BeEquivalentTo(data);

            var updated = new UpdatedCachePlayer
            {
                Id = new Guid(key),
                Level = 123,
                LikeCount = 555
            };

            await cacheManager.HashSetAsync(cacheType, key, updated);

            var updatedCachePlayer = await cacheManager.HashGetAsync<UpdatedCachePlayer>(cacheType, key);

            updatedCachePlayer.Id.Should().Be(data.Id);
            updatedCachePlayer.Level.Should().Be(data.Level);
            updatedCachePlayer.LikeCount.Should().Be(updated.LikeCount);

            redisData = await cacheManager.HashGetAsync<CachePlayer>(cacheType, key);

            redisData.Should().BeEquivalentTo(data);
        }

        [Test]
        public async Task CanHashIncrement()
        {
            const string cacheType = "TestHashSet3";

            var key = "2041038b-dedb-4ba5-bdb0-db7f44c37312";

            var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

            var updated = new UpdatedCachePlayer
            {
                Id = new Guid(key),
                Level = 123,
                LikeCount = 555
            };

            await cacheManager.HashSetAsync(cacheType, key, updated);

            var likeCount =
                await cacheManager.HashFieldIncrementAsync(cacheType, key, nameof(UpdatedCachePlayer.LikeCount));

            likeCount.Should().Be(updated.LikeCount + 1);

            var updatedCachePlayer = await cacheManager.HashGetAsync<UpdatedCachePlayer>(cacheType, key);

            updatedCachePlayer.Id.Should().Be(updated.Id);
            updatedCachePlayer.Level.Should().Be(updated.Level);
            updatedCachePlayer.LikeCount.Should().Be(updated.LikeCount + 1);
        }

        [Test]
        public async Task CanHashSetByField()
        {
            const string cacheType = "TestHashSet4";

            var key = "2041038b-dedb-4ba5-bdb0-db7f44c37312";

            var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

            var updated = new UpdatedCachePlayer
            {
                Id = new Guid(key),
                Level = 123,
                LikeCount = 555
            };

            await cacheManager.HashSetAsync(cacheType, key, updated);


            await cacheManager.HashSetFieldsAsync(cacheType, key, new Dictionary<string, object>
            {
                {nameof(UpdatedCachePlayer.LikeCount), 667}
            });


            var updatedCachePlayer = await cacheManager.HashGetAsync<UpdatedCachePlayer>(cacheType, key);

            updatedCachePlayer.Id.Should().Be(updated.Id);
            updatedCachePlayer.Level.Should().Be(updated.Level);
            updatedCachePlayer.LikeCount.Should().Be(667);
        }

        [Test]
        public async Task CanSetAddAndGetAll()
        {
            const string cacheType = "testset1";


            var cacheManager = DefaultFactory.GetService<IRedisCacheManager>();

            var value1 = new Guid("F92652A0-6EBD-49B1-93D7-EB0A9CB379CE");
            var value2 = new Guid("F92652A0-6EBD-49B1-93D7-EB0A9CB379CE");
            var value3 = new Guid("F92652A0-6EBD-49B1-93D7-EB0A9CB379C1");
            var value4 = new Guid("F92652A0-6EBD-49B1-93D7-EB0A9CB379C2");

            await cacheManager.SetAddAsync(cacheType, string.Empty, value1);
            await cacheManager.SetAddAsync(cacheType, string.Empty, value2);
            await cacheManager.SetAddAsync(cacheType, string.Empty, value3);
            await cacheManager.SetAddAsync(cacheType, string.Empty, value4);


            var data = await cacheManager.SetGetAllAsync<Guid>(cacheType, string.Empty);

            data.Length.Should().Be(3);
            data.Any(a => a.Equals(value1)).Should().BeTrue();
            data.Any(a => a.Equals(value3)).Should().BeTrue();
            data.Any(a => a.Equals(value4)).Should().BeTrue();

            await cacheManager.SetRemoveAsync(cacheType, string.Empty, value2);

            data = await cacheManager.SetGetAllAsync<Guid>(cacheType, string.Empty);

            data.Length.Should().Be(2);
            data.Any(a => a.Equals(value1)).Should().BeFalse();
            data.Any(a => a.Equals(value3)).Should().BeTrue();
            data.Any(a => a.Equals(value4)).Should().BeTrue();
        }
    }

    class UpdatedCachePlayer
    {
        public Guid Id { get; set; }

        public int Level { get; set; }

        public int LikeCount { get; set; }
    }

    public interface ITestQueries
    {
        Task<string> GetDataAsync();
    }
}