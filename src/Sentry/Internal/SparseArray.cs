namespace Sentry.Internal;

/// <summary>
/// Sparse array for scalars (value types). You must provide the uninitialized value that will be used for new unused elements.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class SparseScalarArray<T> where T : IEquatable<T>
{
    private GrowableArray<T> _items;
    private T _uninitializedValue;

    public SparseScalarArray(T uninitializedValue)
    {
        _items = new GrowableArray<T>();
        _uninitializedValue = uninitializedValue;
    }

    public SparseScalarArray(T uninitializedValue, int capacity)
    {
        _items = new GrowableArray<T>(capacity);
        _uninitializedValue = uninitializedValue;
    }

    public T this[int index]
    {
        get
        {
            return _items[index];
        }
        set
        {
            // Increase the capacity of the sparse array so that the key can fit.
            while (_items.Count <= index)
            {
                _items.Add(_uninitializedValue);
            }
            _items[index] = value;
        }
    }

    public bool ContainsKey(int key)
    {
        return key > 0 && key < _items.Count && !_uninitializedValue.Equals(_items[key]);
    }
}

/// <summary>
/// Sparse array. Null value is considered a missing item.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class SparseArray<T>
{
    private GrowableArray<T?> _items;

    public SparseArray()
    {
        _items = new GrowableArray<T?>();
    }

    public SparseArray(int capacity)
    {
        _items = new GrowableArray<T?>(capacity);
    }

    public T? this[int index]
    {
        get
        {
            return _items[index];
        }
        set
        {
            // Increase the capacity of the sparse array so that the key can fit.
            _items.Reserve(index);
            while (_items.Count <= index)
            {
                _items.Add(default);
            }
            _items[index] = value;
        }
    }

    public bool ContainsKey(int key)
    {
        return key > 0 && key < _items.Count && _items[key] is not null;
    }

    /// <summary>
    /// Executes 'func(key, value)' for each element present.
    /// </summary>
    public void Foreach(Action<int, T> func)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] is { } value)
            {
                func(i, value);
            }
        }
    }

    public List<int> Keys()
    {
        var list = new List<int>();
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] is { } value)
            {
                list.Add(i);
            }
        }
        return list;
    }
}
