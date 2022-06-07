using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Sentry.Internal.Extensions
{
    internal static class CollectionsExtensions
    {
        public static TValue GetOrCreate<TValue>(
            this ConcurrentDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                if (value is TValue casted)
                {
                    return casted;
                }

                throw new($"Expected a type of {nameof(TValue)} to exist for the key '{key}'. Instead found a {value.GetType()}");
            }

            return new TValue();
        }

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

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull =>
            source.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public static IEnumerable<KeyValuePair<TKey, TValue>> WhereNotNullValue<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue?>> source) where TKey : notnull
        {
            foreach (var kvp in source)
            {
                if (kvp.Value is not null)
                {
                    yield return kvp!;
                }
            }
        }
    }
}
