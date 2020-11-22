using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Internal.Extensions
{
    internal static class CollectionsExtensions
    {
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (var i in items)
            {
                collection.Add(i);
            }
        }

        public static TValue GetOrCreate<TValue>(
            this ConcurrentDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
            => (TValue) dictionary.GetOrAdd(key, _ => new TValue());

        public static void TryCopyTo<TKey, TValue>(this IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
            where TKey : notnull
        {
            foreach (var (key, value) in from)
            {
                if (!to.ContainsKey(key))
                {
                    to[key] = value;
                }
            }
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
                where TKey : notnull =>
            source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}
