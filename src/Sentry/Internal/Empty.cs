using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sentry.Internal
{
    internal static class Empty
    {
        private static class DictionaryContainer<TKey, TValue> where TKey : notnull
        {
            public static IReadOnlyDictionary<TKey, TValue> Instance { get; } =
                new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>());
        }

        public static IReadOnlyDictionary<TKey, TValue> Dictionary<TKey, TValue>() where TKey : notnull =>
            DictionaryContainer<TKey, TValue>.Instance;
    }
}
