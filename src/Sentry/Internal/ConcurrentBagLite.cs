namespace Sentry.Internal;

/// <summary>
/// A minimal replacement for <see cref="ConcurrentBag{T}"/>.
///
/// We're using this to avoid the same class of memory leak that <see cref="ConcurrentQueueLite{T}"/>
/// was introduced to avoid. See https://github.com/getsentry/sentry-dotnet/issues/5113
/// </summary>
internal class ConcurrentBagLite<T> : IEnumerable<T>
{
    private readonly List<T> _items;

    public ConcurrentBagLite()
    {
        _items = new List<T>();
    }

    public ConcurrentBagLite(IEnumerable<T> collection)
    {
        _items = new List<T>(collection);
    }

    public void Add(T item)
    {
        lock (_items)
        {
            _items.Add(item);
        }
    }

    public int Count
    {
        get
        {
            lock (_items)
            {
                return _items.Count;
            }
        }
    }

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        lock (_items)
        {
            _items.Clear();
        }
    }

    public T[] ToArray()
    {
        lock (_items)
        {
            return _items.ToArray();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        // Return a snapshot to avoid holding the lock during iteration
        // and to prevent InvalidOperationException if the collection is modified.
        T[] snapshot;
        lock (_items)
        {
            snapshot = _items.ToArray();
        }
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
