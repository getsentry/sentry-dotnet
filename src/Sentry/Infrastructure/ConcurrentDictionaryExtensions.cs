using System.Collections.Concurrent;

namespace Sentry.Infrastructure
{
    internal static class ConcurrentDictionaryExtensions
    {
        public static TValue GetOrCreate<TValue>(
            this ConcurrentDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
            => dictionary.GetOrAdd(key, _ => new TValue()) as TValue;
    }
}
