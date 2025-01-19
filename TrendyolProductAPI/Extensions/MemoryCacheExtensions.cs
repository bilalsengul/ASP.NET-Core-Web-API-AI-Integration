using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.Caching.Memory;

namespace TrendyolProductAPI.Extensions
{
    public static class MemoryCacheExtensions
    {
        private static readonly ConcurrentDictionary<string, bool> _keys = new ConcurrentDictionary<string, bool>();

        public static void AddKey(this IMemoryCache cache, string key)
        {
            _keys.TryAdd(key, true);
        }

        public static void RemoveKey(this IMemoryCache cache, string key)
        {
            _keys.TryRemove(key, out _);
        }

        public static IEnumerable<string> GetKeys<T>(this IMemoryCache cache)
        {
            return _keys.Keys;
        }
    }
} 