using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using Microsoft.Extensions.Caching.Redis;

namespace NetworkBase.Extensions
{
    public static class CacheExtensions
    {
        public struct SortedSetEntryHelper<T>
        {
            public T Value
            {
                get;
                set;
            }

            public double Score
            {
                get;
                set;
            }
        }

        public static async Task<T> GetObjectAsync<T>(this IDistributedCache cache, string key)
        {
            var bytes = await cache.GetAsync(key);
            if (bytes == null)
            {
                return default(T);
            }
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T obj)
        {
            var bytes = GetBytesFromObject(obj);
            await cache.SetAsync(key, bytes);
        }

        public static async Task SetObjectAsync<T>(this IDistributedCache cache, string key, T obj, DistributedCacheEntryOptions opts)
        {
            var bytes = GetBytesFromObject(obj);
            await cache.SetAsync(key, bytes, opts);
        }

        public static async Task<T[]> GetSetObjectsAsync<T>(this RedisCache cache, string key)
        {
            var bytes = await cache.GetSetMembersAsync(key);
            var len = bytes.Length;
            if (len == 0)
            {
                return null;
            }

            var ret = new T[len];
            for (int i = 0; i < len; ++i)
            {
                ret[i] = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes[i]));
            }

            return ret;
        }

        public static async Task<T[]> GetSortedSetObjectsByRankAsync<T>(this RedisCache cache, string key, long start, long end)
        {
            var bytes = await cache.GetSortedSetRangeByRankAsync(key, start, end);
            var len = bytes.Length;
            if (len == 0)
            {
                return null;
            }

            var ret = new T[len];
            for (int i = 0; i < len; ++i)
            {
                ret[i] = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes[i]));
            }

            return ret;
        }

        public static async Task<SortedSetEntryHelper<T>[]> GetSortedSetObjectsByRankWithScoresAsync<T>(this RedisCache cache, string key, long start, long end)
        {
            var sets = await cache.GetSortedSetRangeByRankWithScoresAsync(key, start, end);
            var len = sets.Length;
            if (len == 0)
            {
                return null;
            }

            var ret = new SortedSetEntryHelper<T>[len];
            for (int i = 0; i < len; ++i)
            {
                ret[i].Value = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString((byte[])sets[i].Element));
                ret[i].Score = (double)sets[i].Score;
            }

            return ret;
        }

        public static async Task<T[]> GetSortedSetObjectsByScoreAsync<T>(this RedisCache cache, string key, long start, long end)
        {
            var bytes = await cache.GetSortedSetRangeByScoreAsync(key, start, end);
            var len = bytes.Length;
            if (len == 0)
            {
                return null;
            }

            var ret = new T[len];
            for (int i = 0; i < len; ++i)
            {
                ret[i] = JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes[i]));
            }

            return ret;
        }

        public static async Task<bool> AddSetObjectAsync<T>(this RedisCache cache, string key, T obj)
        {
            var bytes = GetBytesFromObject(obj);
            return await cache.AddSetAsync(key, bytes);
        }

        public static async Task AddSortedSetObjectAsync<T>(this RedisCache cache, string key, double score, T obj)
        {
            var bytes = GetBytesFromObject(obj);
            await cache.AddSortedSetAsync(key, score, bytes, new DistributedCacheEntryOptions());
        }

        public static async Task AddSortedSetObjectAsync<T>(this RedisCache cache, string key, double score, T obj, DistributedCacheEntryOptions opts)
        {
            var bytes = GetBytesFromObject(obj);
            await cache.AddSortedSetAsync(key, score, bytes, opts);
        }

        public static async Task<bool> RemoveSetObjectAsync<T>(this RedisCache cache, string key, T obj)
        {
            var bytes = GetBytesFromObject(obj);
            return await cache.RemoveSetAsync(key, bytes);
        }

        private static byte[] GetBytesFromObject<T>(T obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
    }
}
