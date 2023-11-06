namespace Sentry.Internal;

internal abstract class ScopeSyncingDictionary <K, V> : IDictionary<K, V>
{
    private readonly IDictionary<K, V> _innerDictionary;
    protected readonly SentryOptions Options;

    public ScopeSyncingDictionary(
        IDictionary<K, V> innerDictionary,
        SentryOptions options
        )
    {
        _innerDictionary = innerDictionary ?? throw new ArgumentNullException(nameof(innerDictionary));
        Options = options;
    }

    public bool TryGetValue(K key, out V value)
    {
        return _innerDictionary.TryGetValue(key, out value!);
    }

    public abstract void SyncSetValue(K key, V value);
    public virtual bool FilterValue(K key, V value) => false;
    public abstract void SyncRemoveValue(K key);

    public V this[K key]
    {
        get => _innerDictionary[key];
        set
        {
            if (FilterValue(key, value))
            {
                return;
            }
            _innerDictionary[key] = value;
            if (Options.EnableScopeSync)
            {
                SyncSetValue(key, value);
            }
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
        if (FilterValue(key, value))
        {
            return;
        }
        _innerDictionary.Add(key, value);
        if (Options.EnableScopeSync)
        {
            SyncSetValue(key, value);
        }
    }

    public bool Remove(K key)
    {
        // Check if the call is intercepted
        var removed = _innerDictionary.Remove(key);
        if (Options.EnableScopeSync)
        {
            SyncRemoveValue(key);
        }
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
        if (Options.EnableScopeSync)
        {
            // Workaround for the lack Clear methods in the SentryCocoaSdk
            foreach (var key in Keys)
            {
                SyncRemoveValue(key);
            }
        }
        _innerDictionary.Clear();
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
