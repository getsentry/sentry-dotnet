using System;
using System.Collections.Generic;

// ReSharper disable LoopCanBePartlyConvertedToQuery
// ReSharper disable LoopCanBeConvertedToQuery

namespace Sentry.NLog
{
    internal static class HelperExtensions
    {
        internal static IEnumerable<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector)
        {
            foreach (var item in source)
            {
                yield return new KeyValuePair<TKey, TValue>(keySelector(item), valueSelector(item));
            }
        }

        internal static IEnumerable<T> DistinctBy<T, TProp>(this IEnumerable<T> collection, Func<T, TProp> selector)
        {
            var found = new HashSet<TProp>();

            foreach (var item in collection)
            {
                if (found.Add(selector(item)))
                {
                    yield return item;
                }
            }

        }
    }
}
