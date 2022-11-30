using System;
using System.Diagnostics;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace LSG.SharedKernel.Redis
{
    public interface IRedisLockSummary : IDisposable
    {
        bool IsAcquired { get; }
        bool IsReleased { get; }
    }

    public interface IRedisLock
    {
        Task<TResult> ExecuteLockAsync<TResult>(string resource, TimeSpan expiryTime,
            Func<IRedisLockSummary, Task<TResult>> execFunc, Func<IRedisLockSummary, TResult> handleFailureFunc,
            TimeSpan? waitTime = null, TimeSpan? retryInterval = null);

        Task ExecuteLockAsync(string resource, TimeSpan expiryTime, Func<Task> execFunc,
            Func<Task> handleFailureFunc = null, TimeSpan? waitTime = null, TimeSpan? retryInterval = null);
    }

    public sealed class RedisLock : IRedisLock
    {
        private readonly IRedisConnection _redisConnection;

        public RedisLock(IRedisConnection redisConnection)
        {
            _redisConnection = redisConnection;
        }

        private async Task<IRedisLockSummary> CreateLockAsync(string resource, TimeSpan expiryTime)
        {
            var token = Guid.NewGuid().ToString();
            var key = $"RedisLock:{resource}";
            var locker = new InternalLock(key, token, expiryTime, _redisConnection.Connection.GetDatabase());
            await locker.LockAsync();
            return locker;
        }

        async Task<TResult> IRedisLock.ExecuteLockAsync<TResult>(string resource, TimeSpan expiryTime,
            Func<IRedisLockSummary, Task<TResult>> execFunc, Func<IRedisLockSummary, TResult> handleFailureFunc,
            TimeSpan? waitTime, TimeSpan? retryInterval)
        {
            if (execFunc == null || handleFailureFunc == null)

                throw new ArgumentNullException();

            var needRetry = waitTime.HasValue && retryInterval.HasValue && waitTime.Value.TotalMilliseconds > 0 &&
                            retryInterval.Value.TotalMilliseconds > 0;

            TResult res;

            var stopwatch = Stopwatch.StartNew();

            do
            {
                var (locker, result) = await AcquireAsync();
                res = result;
                if (locker.IsAcquired)
                {
                    return result;
                }

                if (!needRetry)
                    return result;

                await Task.Delay(retryInterval.Value);
            } while (stopwatch.Elapsed <= waitTime.Value);

            return res;

            async Task<(IRedisLockSummary summary, TResult result)> AcquireAsync()
            {
                using (var locker = await CreateLockAsync(resource, expiryTime))
                {
                    return locker.IsAcquired ? (locker, await execFunc(locker)) : (locker, handleFailureFunc(locker));
                }
            }
        }

        async Task IRedisLock.ExecuteLockAsync(string resource, TimeSpan expiryTime, Func<Task> execFunc,
            Func<Task> handleFailedFunc, TimeSpan? waitTime, TimeSpan? retryInterval)
        {
            if (execFunc == null)
                throw new ArgumentNullException();

            var needRetry = waitTime.HasValue && retryInterval.HasValue && waitTime.Value.TotalMilliseconds > 0 &&
                            retryInterval.Value.TotalMilliseconds > 0;

            var stopwatch = Stopwatch.StartNew();

            do
            {
                var locker = await AcquireAsync();

                if (locker.IsAcquired)
                {
                    return;
                }

                if (!needRetry)
                    return;

                await Task.Delay(retryInterval.Value);
            } while (stopwatch.Elapsed <= waitTime.Value);

            async Task<IRedisLockSummary> AcquireAsync()
            {
                using (var locker = await CreateLockAsync(resource, expiryTime))
                {
                    if (!locker.IsAcquired)
                    {
                        if (handleFailedFunc != null)
                            await handleFailedFunc();
                        return locker;
                    }

                    await execFunc();
                    return locker;
                }
            }
        }

        private class InternalLock : IRedisLockSummary
        {
            private readonly IDatabase _database;
            private readonly string _key;
            private readonly string _token;
            private readonly TimeSpan _expiryTime;

            public InternalLock(string key, string token, TimeSpan expiryTime, IDatabase database)
            {
                _key = key;
                _token = token;
                _database = database;
                _expiryTime = expiryTime;
            }

            public async Task LockAsync()
            {
                IsAcquired = await _database.LockTakeAsync(_key, _token, _expiryTime);
            }

            public void Dispose()
            {
                IsReleased = _database.LockRelease(_key, _token);
            }

            public bool IsAcquired { get; private set; }

            public bool IsReleased { get; private set; }
        }
    }
}