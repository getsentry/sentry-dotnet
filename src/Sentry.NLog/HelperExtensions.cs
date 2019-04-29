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
            if (source is null)
                yield break;

            foreach (var item in source)
            {
                yield return new KeyValuePair<TKey, TValue>(keySelector(item), valueSelector(item));
            }
        }

        internal static IEnumerable<KeyValuePair<TKey, TNewValue>> MapKeys<TKey, TValue, TNewValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, Func<TValue,TNewValue> selector)
        {
            if (source is null)
                yield break;

            foreach (var item in source)
            {
                yield return new KeyValuePair<TKey, TNewValue>(item.Key, selector(item.Value));
            }

        }

        internal static IEnumerable<T> DistinctBy<T, TProp>(this IEnumerable<T> collection, Func<T, TProp> selector)
        {
            if (collection is null)
                yield break;

            var found = new HashSet<TProp>();

            foreach (var item in collection)
            {
                if (found.Add(selector(item)))
                {
                    yield return item;
                }
            }
        }

        internal static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            if (source is null || items is null)
                return;

            foreach (var item in items)
            {
                source.Add(item);
            }
        }
    }
}
