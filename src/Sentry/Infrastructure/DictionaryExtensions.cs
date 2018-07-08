using System.Collections.Generic;

namespace Sentry.Infrastructure
{
    internal static class DictionaryExtensions
    {
        public static TValue GetOrCreate<TValue>(
            this IDictionary<string, object> dictionary,
            string key)
            where TValue : class, new()
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value as TValue;
            }

            var context = new TValue();
            dictionary[key] = context;
            return context;
        }
    }
}
