using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Sentry.Protocol
{
    internal static class CollectionsExtensions
    {
        public static TValue GetOrCreate<TValue>(
            this ConcurrentDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
            => (TValue) dictionary.GetOrAdd(key, _ => new TValue());

        public static ConcurrentQueue<T> EnqueueAll<T>(this ConcurrentQueue<T> target, IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                target.Enqueue(value);
            }

            return target;
        }

        public static void TryCopyTo<TKey, TValue>(this IDictionary<TKey, TValue> from, IDictionary<TKey, TValue> to)
            where TKey : notnull
        {
            foreach (var kv in from)
            {
                if (!to.ContainsKey(kv.Key))
                {
                    to[kv.Key] = kv.Value;
                }
            }
        }
    }
}
