using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace Sentry.Internal;

/// <summary>
/// Provides a keyed set of counters that can be incremented, read, and reset atomically.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
internal class ThreadsafeCounterDictionary<TKey> : IReadOnlyDictionary<TKey, int>
    where TKey : notnull
{
    private class CounterItem
    {
        private int _value;
        public int Value => _value;

        public void Add(int quantity) => Interlocked.Add(ref _value, quantity);
        public void Increment() => Interlocked.Increment(ref _value);
        public int ReadAndReset() => Interlocked.Exchange(ref _value, 0);
    }

    private readonly ConcurrentDictionary<TKey, CounterItem> _items = new();

    /// <summary>
    /// Atomically adds to a counter based on the key provided, creating the counter if necessary.
    /// </summary>
    /// <param name="key">The key of the counter to increment.</param>
    /// <param name="quantity">The amount to add to the counter.</param>
    public void Add(TKey key, int quantity) => _items.GetOrAdd(key, new CounterItem()).Add(quantity);

    /// <summary>
    /// Atomically increments a counter based on the key provided, creating the counter if necessary.
    /// </summary>
    /// <param name="key">The key of the counter to increment.</param>
    public void Increment(TKey key) => _items.GetOrAdd(key, new CounterItem()).Increment();

    /// <summary>
    /// Gets a single counter's value while atomically resetting it to zero.
    /// </summary>
    /// <param name="key">The key to the counter.</param>
    /// <returns>The previous value of the counter.</returns>
    /// <remarks>If no counter with the given key has been set, this returns zero.</remarks>
    public int ReadAndReset(TKey key) => _items.TryGetValue(key, out var item) ? item.ReadAndReset() : 0;

    /// <summary>
    /// Gets the keys and values of all of the counters while atomically resetting them to zero.
    /// </summary>
    /// <returns>A read-only dictionary containing the key and the previous value for each counter.</returns>
    public IReadOnlyDictionary<TKey, int> ReadAllAndReset()
    {
        // Read all the counters while atomically resetting them to zero
        var counts = _items.ToDictionary(
            x => x.Key,
            x => x.Value.ReadAndReset());

        return new ReadOnlyDictionary<TKey, int>(counts);
    }

    /// <summary>
    /// Gets an enumerator over the keys and values of the counters.
    /// </summary>
    /// <returns>An enumerator.</returns>
    public IEnumerator<KeyValuePair<TKey, int>> GetEnumerator() => _items
        .Select(x => new KeyValuePair<TKey, int>(x.Key, x.Value.Value))
        .GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Gets the number of counters currently being tracked.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Tests whether or not a counter with the given key exists.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the counter exists, false otherwise.</returns>
    public bool ContainsKey(TKey key) => _items.ContainsKey(key);

    /// <summary>
    /// Gets the current value of the counter specified.
    /// </summary>
    /// <param name="key">The key of the counter.</param>
    /// <param name="value">The value of the counter, or zero if the counter does not yet exist.</param>
    /// <returns>Returns <c>true</c> in all cases.</returns>
    public bool TryGetValue(TKey key, out int value)
    {
        value = this[key];
        return true;
    }

    /// <summary>
    /// Gets the current value of the counter specified, returning zero if the counter does not yet exist.
    /// </summary>
    /// <param name="key">The key of the counter.</param>
    public int this[TKey key] => _items.TryGetValue(key, out var item) ? item.Value : 0;

    /// <summary>
    /// Gets all of the current counter keys.
    /// </summary>
    public IEnumerable<TKey> Keys => _items.Keys;

    /// <summary>
    /// Gets all of the current counter values.
    /// </summary>
    /// <remarks>
    /// Useless, but required by the IReadOnlyDictionary interface.
    /// </remarks>
    public IEnumerable<int> Values => _items.Values.Select(x => x.Value);
}
