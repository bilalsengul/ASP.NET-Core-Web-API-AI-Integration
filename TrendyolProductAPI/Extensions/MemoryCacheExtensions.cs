using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Reflection;

namespace TrendyolProductAPI.Extensions
{
    public static class MemoryCacheExtensions
    {
        public static IEnumerable<string> GetKeys<T>(this IMemoryCache cache)
        {
            var field = typeof(MemoryCache).GetProperty("EntriesCollection", BindingFlags.NonPublic | BindingFlags.Instance);
            var collection = field?.GetValue(cache) as ICollection;
            
            if (collection == null) return Enumerable.Empty<string>();
            
            var items = new List<string>();
            foreach (var item in collection)
            {
                var methodInfo = item.GetType().GetProperty("Key");
                var key = methodInfo?.GetValue(item)?.ToString();
                if (!string.IsNullOrEmpty(key))
                {
                    items.Add(key);
                }
            }
            
            return items;
        }
    }
} 