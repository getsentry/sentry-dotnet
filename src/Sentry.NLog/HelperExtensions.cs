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
            {
                yield break;
            }

            foreach (var item in source)
            {
                yield return new KeyValuePair<TKey, TValue>(keySelector(item), valueSelector(item));
            }
        }

        internal static IEnumerable<KeyValuePair<TKey, TNewValue>> MapValues<TKey, TValue, TNewValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source,
            Func<TValue,TNewValue> valueSelector)
        {
            if (source is null)
            {
                yield break;
            }

            foreach (var item in source)
            {
                yield return new KeyValuePair<TKey, TNewValue>(item.Key, valueSelector(item.Value));
            }

        }

        internal static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            if (source is null || items is null)
            {
                return;
            }

            foreach (var item in items)
            {
                source.Add(item);
            }
        }
    }
}
