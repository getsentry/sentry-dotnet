namespace Sentry.Internal;

internal class InterceptingDictionary <K, V> : IDictionary<K, V>
{
    private readonly IDictionary<K, V> _innerDictionary;
    private readonly Func<K, V, bool>? _beforeSet;
    private readonly Func<K, bool>? _beforeRemove;
    private readonly Action<K, V>? _afterSet;
    private readonly Action<K>? _afterRemove;
    private readonly Func<bool>? _beforeClear;
    private readonly Action? _afterClear;

    public InterceptingDictionary(
        IDictionary<K, V> innerDictionary,
        Func<K, V, bool>? beforeSet = null,
        Func<K, bool>? beforeRemove = null,
        Action<K, V>? afterSet = null,
        Action<K>? afterRemove = null,
        Func<bool>? beforeClear = null,
        Action? afterClear = null
        )
    {
        _innerDictionary = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));
        _beforeSet = beforeSet;
        _beforeRemove = beforeRemove;
        _afterSet = afterSet;
        _afterRemove = afterRemove;
        _beforeClear = beforeClear;
        _afterClear = afterClear;
    }

    public bool TryGetValue(K key, out V value)
    {
        return _innerDictionary.TryGetValue(key, out value!);
    }

    public V this[K key]
    {
        get => _innerDictionary[key];
        set
        {
            if (_beforeSet != null && !_beforeSet(key, value))
            {
                return;
            }
            _innerDictionary[key] = value;
            _afterSet?.Invoke(key, value);
        }
    }

    public ICollection<K> Keys => _innerDictionary.Keys;

    public ICollection<V> Values => _innerDictionary.Values;

    public bool ContainsKey(K key)
    {
        return _innerDictionary.ContainsKey(key);
    }

    public void Add(K key, V value)
    {
        if (_beforeSet != null && !_beforeSet(key, value))
        {
            return;
        }
        _innerDictionary.Add(key, value);
        _afterSet?.Invoke(key, value);
    }

    public bool Remove(K key)
    {
        // Check if the call is intercepted
        if (_beforeRemove != null && !_beforeRemove(key))
        {
            return false;
        }
        var removed = _innerDictionary.Remove(key);
        _afterRemove?.Invoke(key);
        return removed;
    }

    public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
    {
        return _innerDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_innerDictionary).GetEnumerator();
    }

    public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

    public void Clear()
    {
        // Check if the call is intercepted
        if (_beforeClear != null && !_beforeClear())
        {
            return;
        }
        _innerDictionary.Clear();
        _afterClear?.Invoke();
    }

    public bool Contains(KeyValuePair<K, V> item)
    {
        return _innerDictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex)
    {
        _innerDictionary.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<K, V> item) => Remove(item.Key);

    public int Count => _innerDictionary.Count;

    public bool IsReadOnly => _innerDictionary.IsReadOnly;
}
