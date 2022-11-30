using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using LSG.Core;
using LSG.Hosts.LsgApi;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Redis;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LSG.IntegrationTests.Redis
{
    [TestFixture]
    [Category(Const.TestCategory.Integration)]
    public class RedisLockTests : Base<LsgApiStartup>
    {
        private Func<Type, IRedisLock> _redisLockFactory;


        public override async Task Init()
        {
            await base.Init();
            _redisLockFactory = DefaultFactory.GetRequiredService<Func<Type, IRedisLock>>();
        }


        [TestCase(typeof(RedisConnection))]
        public async Task TestSingleLock(Type type)
        {
            var redisLock = _redisLockFactory(type);

            var resource = $"testredislock:{Guid.NewGuid()}";
            IRedisLockSummary firstLock = null;

            await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                c =>
                {
                    firstLock = c;
                    firstLock.IsAcquired.Should().BeTrue();
                    firstLock.IsReleased.Should().BeFalse();
                    return Task.FromResult(true);
                }, _ => true);

            firstLock.IsReleased.Should().BeTrue();
        }

        [TestCase(typeof(RedisConnection))]
        public async Task TestOverlappingLocks(Type type)
        {
            var redisLock = _redisLockFactory(type);
            var resource = $"testredislock:{Guid.NewGuid()}";

            IRedisLockSummary firstLock = null;
            await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                async c =>
                {
                    firstLock = c;
                    firstLock.IsAcquired.Should().BeTrue();
                    IRedisLockSummary secondLock = null;
                    await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                        cc => Task.FromResult(true), cc =>
                        {
                            secondLock = cc;
                            secondLock.IsAcquired.Should().BeFalse();
                            firstLock.IsReleased.Should().BeFalse();
                            return true;
                        });
                    secondLock.IsReleased.Should().BeFalse();
                    return true;
                }, _ => true);

            firstLock.IsReleased.Should().BeTrue();
        }

        [TestCase(typeof(RedisConnection))]
        public async Task TestSequentialLocks(Type type)
        {
            var redisLock = _redisLockFactory(type);
            var resource = $"testredislock:{Guid.NewGuid()}";

            IRedisLockSummary firstLock = null;

            await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                c =>
                {
                    firstLock = c;
                    firstLock.IsAcquired.Should().BeTrue();
                    firstLock.IsReleased.Should().BeFalse();
                    return Task.FromResult(true);
                }, _ => true);

            firstLock.IsReleased.Should().BeTrue();

            await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                c =>
                {
                    firstLock = c;
                    firstLock.IsAcquired.Should().BeTrue();
                    firstLock.IsReleased.Should().BeFalse();
                    return Task.FromResult(true);
                }, _ => true);
            firstLock.IsReleased.Should().BeTrue();
        }

        [TestCase(typeof(RedisConnection))]
        public async Task TestLockReleasedAfterTimeout(Type type)
        {
            var redisLock = _redisLockFactory(type);
            var resource = $"testredislock:{Guid.NewGuid()}";

            var expiredTime = TimeSpan.FromSeconds(3);
            IRedisLockSummary firstLock = null;

            await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(3),
                async c =>
                {
                    firstLock = c;
                    firstLock.IsAcquired.Should().BeTrue();
                    var waitTime = expiredTime.Add(TimeSpan.FromSeconds(1));
                    await Task.Delay(waitTime);
                    IRedisLockSummary secondLock = null;
                    await redisLock.ExecuteLockAsync(resource, TimeSpan.FromSeconds(10),
                        cc =>
                        {
                            secondLock = cc;
                            secondLock.IsAcquired.Should().BeTrue();
                            return Task.FromResult(true);
                        }, cc => true);
                    secondLock.IsReleased.Should().BeTrue();
                    return true;
                }, _ => true);

            firstLock.IsReleased.Should().BeFalse();
        }

        [TestCase(typeof(RedisConnection), 10), Category(Const.TestCategory.LocalOnly)]
        public async Task TestBlockingConcurrentLocks(Type type, int totalTask)
        {
            var redisLock = _redisLockFactory(type);
            var resource = $"testredislock:{Guid.NewGuid()}";

            var expiredTime = TimeSpan.FromSeconds(60);
            var lockCount = 0;
            var faillockCount = 0;

            var blocker = new AutoResetEvent(false);

            var list = new List<Task>();

            for (var i = 0; i < totalTask; i++)
            {
                list.Add(LockAsync());
            }

            Console.WriteLine($@" {resource} start task execute at {DateTimeOffset.UtcNow:hh:mm:ss.fff} ");


            await Task.WhenAll(list).TimeoutAfterAsync(TimeSpan.FromSeconds(10));


            lockCount.Should().Be(1);
            faillockCount.Should().Be(list.Count - 1);

            Task LockAsync()
            {
                return redisLock.ExecuteLockAsync(resource, expiredTime,
                    c =>
                    {
                        lockCount++;

                        Console.WriteLine($@" {resource} got lock at {DateTimeOffset.UtcNow:hh:mm:ss.fff} ");
                        //keep lock
                        blocker.WaitOne();

                        return Task.FromResult(true);
                    }, _ =>
                    {
                        Console.WriteLine($@" {resource} failed to get lock at {DateTimeOffset.UtcNow:hh:mm:ss.fff} ");

                        blocker.Set();

                        faillockCount++;
                        if (faillockCount + lockCount == totalTask)
                        {
                            Console.WriteLine(
                                $@"failed lock counts: {faillockCount} at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                        }

                        return true;
                    });
            }
        }

        [TestCase(typeof(RedisConnection))]
        public async Task TestWaitLocks(Type type)
        {
            var redisLock = _redisLockFactory(type);
            var resource = ":::testredislock1:::";

            var expiredTime = TimeSpan.FromSeconds(60);
            var lockCount = 0;
            var faillockCount = 0;

            var blocker = new ManualResetEvent(false);

            var list = new List<Task>
            {
                LockAsync(),
                LockAsync(),
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(6));

                    // release block after time
                    blocker.Set();
                    Console.WriteLine($@"{resource} release lock at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                })
            };

            await Task.WhenAll(list).TimeoutAfterAsync(TimeSpan.FromSeconds(10));

            lockCount.Should().Be(2);
            faillockCount.Should().BeGreaterOrEqualTo(2);

            Task LockAsync()
            {
                return redisLock.ExecuteLockAsync(resource, expiredTime,
                    c =>
                    {
                        lockCount++;

                        Console.WriteLine(
                            $@" {resource} got lock at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                        //keep lock
                        blocker.WaitOne();
                        return Task.FromResult(true);
                    }, _ =>
                    {
                        faillockCount++;

                        Console.WriteLine(
                            $@"failed lock counts( {faillockCount}), retring... at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");

                        return true;
                    }, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            }
        }

        [TestCase(typeof(RedisConnection))]
        public async Task TestWaitLocksNoReturn(Type type)
        {
            var redisLock = _redisLockFactory(type);
            var resource = ":::testredislock2:::";

            var expiredTime = TimeSpan.FromSeconds(60);
            var lockCount = 0;
            var faillockCount = 0;

            var blocker = new ManualResetEvent(false);

            var list = new List<Task>
            {
                LockAsync(),
                LockAsync(),
                Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(6));

                    // release block after time
                    blocker.Set();
                    Console.WriteLine($@"{resource} release lock at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                })
            };

            await Task.WhenAll(list).TimeoutAfterAsync(TimeSpan.FromSeconds(10));


            lockCount.Should().Be(2);
            faillockCount.Should().BeGreaterOrEqualTo(2);

            Task LockAsync()
            {
                return redisLock.ExecuteLockAsync(resource, expiredTime,
                    () =>
                    {
                        lockCount++;

                        Console.WriteLine(
                            $@" {resource} got lock at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                        //keep lock
                        blocker.WaitOne();
                        return Task.CompletedTask;
                    },
                    () =>
                    {
                        faillockCount++;

                        Console.WriteLine(
                            $@"failed lock counts( {faillockCount}), retring... at {DateTimeOffset.UtcNow.ToString("hh:mm:ss.fff")} ");
                        return Task.CompletedTask;
                    }, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(1));
            }
        }
    }
}