namespace Sentry.Internal;

/// <summary>
/// Sparse array for scalars (value types). You must provide the uninitialized value that will be used for new unused elements.
/// </summary>
/// <typeparam name="T"></typeparam>
internal sealed class SparseScalarArray<T> where T : IEquatable<T>
{
    private GrowableArray<T> _items;
    private T _uninitializedValue;

    public SparseScalarArray(T uninitializedValue, int capacity = 0)
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
