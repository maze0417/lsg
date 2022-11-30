using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LSG.Core;
using LSG.SharedKernel.Extensions;
using LSG.SharedKernel.Logger;
using StackExchange.Redis;

namespace LSG.SharedKernel.Redis
{
    public interface IRedisCacheManager
    {
        Task<T> SafeStringGetAndCacheAsync<T>(object cacheType, object key, Func<Task<T>> getData,
            TimeSpan? expiresIn = null);

        Task SafeStringCacheAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn = null);

        Task<T> SafeStringGetAsync<T>(object cacheType, object key, Func<Task<T>> getData);

        Task<T> StringGetAsync<T>(object cacheType, object key);

        Task ClearCacheAsync(object cacheType, object key);

        Task<long> IncrementAsync(object cacheType, object key, long value);

        Task HashSetAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn = null)
            where T : class, new();

        Task<T> HashGetAsync<T>(object cacheType, object key)
            where T : class, new();

        Task<long> HashFieldIncrementAsync(object cacheType, object key, string fieldName, long value = 1L);

        Task HashSetFieldsAsync(object cacheType, object key, IDictionary<string, object> hashEntries);

        Task<bool> SetAddAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn = null);

        Task<bool> SetRemoveAsync<T>(object cacheType, object key, T data);

        Task<T[]> SetGetAllAsync<T>(object cacheType, object key);

        Task KeyExpireAsync(object cacheType, object key, TimeSpan? expiresIn);
    }

    public sealed class RedisCacheManager : IRedisCacheManager
    {
        private readonly IRedisConnection _redisConnection;
        private static readonly TimeSpan DefaultTimeSpan = TimeSpan.FromDays(1);
        private readonly ILsgLogger _lsgLogger;


        public RedisCacheManager(IRedisConnection redisConnection, ILsgLogger lsgLogger)
        {
            _redisConnection = redisConnection;
            _lsgLogger = lsgLogger;
        }

        async Task<T> IRedisCacheManager.SafeStringGetAndCacheAsync<T>(object cacheType, object key,
            Func<Task<T>> getData, TimeSpan? expiresIn)
        {
            try
            {
                return await GetAndCacheAsync(cacheType, key, expiresIn, getData);
            }
            catch (Exception ex)
            {
                _lsgLogger.LogError(Const.SourceContext.Redis, ex, "redis error");
                return await getData();
            }
        }

        async Task IRedisCacheManager.SafeStringCacheAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn)
        {
            try
            {
                await CacheAsync(cacheType, key, data, expiresIn);
            }
            catch (Exception ex)
            {
                _lsgLogger.LogError(Const.SourceContext.Redis, ex, "redis error");
            }
        }


        Task IRedisCacheManager.ClearCacheAsync(object cacheType, object key)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            return db.KeyDeleteAsync(cacheKey);
        }


        async Task<T> IRedisCacheManager.SafeStringGetAsync<T>(object cacheType, object key, Func<Task<T>> getData)
        {
            try
            {
                var value = await StringGetAsync(cacheType, key);

                if (value.IsNullOrEmpty)
                {
                    return await getData();
                }

                return JsonSerializer.Deserialize<T>(value);
            }
            catch (Exception ex)
            {
                _lsgLogger.LogError(Const.SourceContext.Redis, ex, "redis error");
                return await getData();
            }
        }

        async Task<T> IRedisCacheManager.StringGetAsync<T>(object cacheType, object key)
        {
            var value = await StringGetAsync(cacheType, key);

            return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value);
        }

        private Task<RedisValue> StringGetAsync(object cacheType, object key)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            return db.StringGetAsync(cacheKey);
        }

        Task<long> IRedisCacheManager.IncrementAsync(object cacheType, object key, long value)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();
            return db.StringIncrementAsync(cacheKey, value);
        }

        async Task IRedisCacheManager.HashSetAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn)
        {
            try
            {
                var cacheKey = GetCacheKey(cacheType, key);
                var db = _redisConnection.Connection.GetDatabase();

                var type = typeof(T);
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    throw new NotSupportedException($"Redis hash set not support IEnumerable type : {type.Name}");
                }

                var propInfo = type.GetPropertyInfo();

                var hashEntries = new List<HashEntry>();

                foreach (var property in propInfo.Properties)
                {
                    var getter = propInfo.ValueGetterByName[property.Name];
                    var value = getter(data);
                    hashEntries.Add(new HashEntry(property.Name, value == null ? string.Empty : value.ToJson()));
                }

                await db.HashSetAsync(cacheKey, hashEntries.ToArray());
                await db.KeyExpireAsync(cacheKey, CapExpiry(expiresIn));
            }
            catch (Exception e)
            {
                _lsgLogger.LogError(Const.SourceContext.Redis, e, "redis error");
                throw;
            }
        }

        async Task<T> IRedisCacheManager.HashGetAsync<T>(object cacheType, object key)
        {
            try
            {
                var cacheKey = GetCacheKey(cacheType, key);
                var db = _redisConnection.Connection.GetDatabase();

                var type = typeof(T);
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    throw new NotSupportedException($"Redis hash set not support IEnumerable type : {type.Name}");
                }

                var propInfo = type.GetPropertyInfo();


                var result = new T();

                var columns = propInfo.Properties.Select(a => new RedisValue(a.Name)).ToArray();
                var values = await db.HashGetAsync(cacheKey, columns);
                var idx = 0;
                foreach (var property in propInfo.Properties)
                {
                    var value = values[idx];
                    idx++;
                    if (value.IsNullOrEmpty)
                    {
                        continue;
                    }

                    var setter = propInfo.ValueSetterByName[property.Name];

                    var val = JsonSerializer.Deserialize(value, property.PropertyType);
                    setter(result, val);
                }

                return result;
            }
            catch (Exception e)
            {
                _lsgLogger.LogError(Const.SourceContext.Redis, e, "redis error");
                throw;
            }
        }

        Task<long> IRedisCacheManager.HashFieldIncrementAsync(object cacheType, object key, string fieldName,
            long value)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            return db.HashIncrementAsync(cacheKey, fieldName, value);
        }

        Task IRedisCacheManager.HashSetFieldsAsync(object cacheType, object key,
            IDictionary<string, object> hashEntries)

        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            var items = hashEntries.Select(a => new HashEntry(a.Key, a.Value.ToJson())).ToArray();
            return db.HashSetAsync(cacheKey, items);
        }

        async Task<bool> IRedisCacheManager.SetAddAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            var added = await db.SetAddAsync(cacheKey, data.ToJson());

            if (added)
            {
                await db.KeyExpireAsync(cacheKey, CapExpiry(expiresIn));
            }

            return added;
        }

        Task<bool> IRedisCacheManager.SetRemoveAsync<T>(object cacheType, object key, T data)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            return db.SetRemoveAsync(cacheKey, data.ToJson());
        }

        async Task<T[]> IRedisCacheManager.SetGetAllAsync<T>(object cacheType, object key)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            var values = await db.SetMembersAsync(cacheKey);
            return !values.Any() ? new T[] { } : values.Select(a => JsonSerializer.Deserialize<T>(a)).ToArray();
        }

        Task IRedisCacheManager.KeyExpireAsync(object cacheType, object key, TimeSpan? expiresIn)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();

            return db.KeyExpireAsync(cacheKey, CapExpiry(expiresIn));
        }

        private async Task<T> GetAndCacheAsync<T>(
            object cacheType, object key, TimeSpan? expiresIn, Func<Task<T>> createFunc)
        {
            var value = await StringGetAsync(cacheType, key);

            if (value.IsNullOrEmpty)
            {
                var createdValue = await createFunc();
                await CacheAsync(cacheType, key, createdValue, expiresIn);
                return createdValue;
            }

            return JsonSerializer.Deserialize<T>(value);
        }

        private Task CacheAsync<T>(object cacheType, object key, T data, TimeSpan? expiresIn)
        {
            var cacheKey = GetCacheKey(cacheType, key);
            var db = _redisConnection.Connection.GetDatabase();
            var serializedValue = data.ToJson();
            return db.StringSetAsync(cacheKey, serializedValue, CapExpiry(expiresIn));
        }

        private static string GetCacheKey(object cacheType, object key)
        {
            return $"{cacheType}:{key}";
        }

        private static TimeSpan CapExpiry(TimeSpan? expiresIn)
        {
            if (expiresIn == null || expiresIn.Value > DefaultTimeSpan)
                return DefaultTimeSpan;
            return expiresIn.Value;
        }
    }
}