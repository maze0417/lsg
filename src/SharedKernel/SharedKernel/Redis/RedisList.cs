using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LSG.SharedKernel.Extensions;
using StackExchange.Redis;

namespace LSG.SharedKernel.Redis
{
    public interface IRedisList<T>
    {
        Task<T[]> GetRangeAsync(long start, long count, string additionalKey = null);

        Task<T[]> TakeAsync(long count, long skip = 0, string additionalKey = null);

        Task AddToStartAsync(T item, string additionalKey = null);

        Task AddToStartAsync(T[] items, string additionalKey = null);

        Task AddToEndAsync(T item, string additionalKey = null);
        Task AddToEndAsync(T[] items, string additionalKey = null);

        Task<T> PopFromStartAsync(string additionalKey = null);

        Task<T> PopFromEndAsync(string additionalKey = null);

        Task ShrinkAsync(int maxSize, string additionalKey = null);

        Task<long> CountAsync(string additionalKey = null);

        Task ClearAsync(string additionalKey = null);

        Task<long> RemoveAsync(T item, string additionalKey = null);
    }

    public sealed class RedisList<T> : IRedisList<T>
    {
        private readonly IRedisList<T> _interface;
        private readonly IRedisConnection _redisConnection;

        public RedisList(
            IRedisConnection redisConnection)
        {
            _interface = this;
            _redisConnection = redisConnection;
        }


        Task IRedisList<T>.AddToStartAsync(T item, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            var value = item.ToJson();
            return db.ListLeftPushAsync(key, value);
        }

        Task IRedisList<T>.AddToStartAsync(T[] items, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            var values = items.Select(x => (RedisValue)x.ToJson()).ToArray();
            return db.ListLeftPushAsync(key, values);
        }

        Task IRedisList<T>.AddToEndAsync(T item, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            var value = item.ToJson();
            return db.ListRightPushAsync(key, value);
        }


        Task IRedisList<T>.AddToEndAsync(T[] items, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            var values = items.Select(x => (RedisValue)x.ToJson()).ToArray();
            return db.ListRightPushAsync(key, values);
        }

        async Task<T> IRedisList<T>.PopFromStartAsync(string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            string value = await db.ListLeftPopAsync(key);
            return string.IsNullOrEmpty(value)
                ? default
                : JsonSerializer.Deserialize<T>(value);
        }

        async Task<T> IRedisList<T>.PopFromEndAsync(string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            string value = await db.ListRightPopAsync(key);
            return string.IsNullOrEmpty(value)
                ? default
                : JsonSerializer.Deserialize<T>(value);
        }

        Task IRedisList<T>.ShrinkAsync(int maxSize, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();


            return db.ListTrimAsync(key, 0, maxSize - 1);
        }

        Task<long> IRedisList<T>.CountAsync(string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            return db.ListLengthAsync(key);
        }

        Task<T[]> IRedisList<T>.TakeAsync(long count, long skip, string additionalKey)
        {
            return _interface.GetRangeAsync(skip, count, additionalKey);
        }

        async Task<T[]> IRedisList<T>.GetRangeAsync(long start, long count, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            var range = await db.ListRangeAsync(key, start, start + count - 1);
            if (range == null)
            {
                return new T[0];
            }

            return range
                .Select(x => JsonSerializer.Deserialize<T>(x))
                .ToArray();
        }

        Task IRedisList<T>.ClearAsync(string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();

            return db.KeyDeleteAsync(key);
        }

        Task<long> IRedisList<T>.RemoveAsync(T item, string additionalKey)
        {
            var key = GetKey(additionalKey);
            var db = _redisConnection.Connection.GetDatabase();
            var value = item.ToJson();
            return db.ListRemoveAsync(key, value);
        }

        string GetKey(string additionalKey)
        {
            return additionalKey.IsNullOrEmpty()
                ? $"{GetType().Name}_{typeof(T).Name}"
                : $"{GetType().Name}_{typeof(T).Name}_{additionalKey}";
        }
    }
}