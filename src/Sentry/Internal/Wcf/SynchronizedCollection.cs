// Adapted from https://github.com/dotnet/wcf/blob/852a6098b270771f21d13824877fce59d3279046/src/System.ServiceModel.Primitives/src/System/ServiceModel/SynchronizedCollection.cs
namespace Sentry.Internal.Wcf;

/// <summary>
/// <para>
/// Provides a thread-safe collection that contains objects of a type specified by the generic parameter as elements.
/// </para>
/// <para>
/// See https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.synchronizedcollection-1?view=net-8.0
/// </para>
/// </summary>
internal class SynchronizedCollection<T> : IList<T>, IList, IReadOnlyCollection<T>
{
    private object _sync;

    public SynchronizedCollection()
    {
        Items = new List<T>();
        _sync = new object();
    }

    public SynchronizedCollection(object syncRoot)
    {
        Items = new List<T>();
        _sync = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
    }

    public SynchronizedCollection(object syncRoot, IEnumerable<T> list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        Items = new List<T>(list);
        _sync = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
    }

    public SynchronizedCollection(object syncRoot, params T[] list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        Items = new List<T>(list.Length);
        for (int i = 0; i < list.Length; i++)
        {
            Items.Add(list[i]);
        }

        _sync = syncRoot ?? throw new ArgumentNullException(nameof(syncRoot));
    }

    public int Count
    {
        get { lock (_sync) { return Items.Count; } }
    }

    protected List<T> Items { get; }

    public object SyncRoot
    {
        get { return _sync; }
    }

    public T this[int index]
    {
        get
        {
            lock (_sync)
            {
                return Items[index];
            }
        }
        set
        {
            lock (_sync)
            {
                if (index < 0 || index >= Items.Count)
                {
                    throw new ArgumentOutOfRangeException();
                }

                SetItem(index, value);
            }
        }
    }

    public void Add(T item)
    {
        lock (_sync)
        {
            int index = Items.Count;
            InsertItem(index, item);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            ClearItems();
        }
    }

    public void CopyTo(T[] array, int index)
    {
        lock (_sync)
        {
            Items.CopyTo(array, index);
        }
    }

    public bool Contains(T item)
    {
        lock (_sync)
        {
            return Items.Contains(item);
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        lock (_sync)
        {
            return Items.GetEnumerator();
        }
    }

    public int IndexOf(T item)
    {
        lock (_sync)
        {
            return InternalIndexOf(item);
        }
    }

    public void Insert(int index, T item)
    {
        lock (_sync)
        {
            if (index < 0 || index > Items.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            InsertItem(index, item);
        }
    }

    private int InternalIndexOf(T item)
    {
        int count = Items.Count;

        for (int i = 0; i < count; i++)
        {
            if (object.Equals(Items[i], item))
            {
                return i;
            }
        }
        return -1;
    }

    public bool Remove(T item)
    {
        lock (_sync)
        {
            int index = InternalIndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveItem(index);
            return true;
        }
    }

    public void RemoveAt(int index)
    {
        lock (_sync)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            RemoveItem(index);
        }
    }

    protected virtual void ClearItems()
    {
        Items.Clear();
    }

    protected virtual void InsertItem(int index, T item)
    {
        Items.Insert(index, item);
    }

    protected virtual void RemoveItem(int index)
    {
        Items.RemoveAt(index);
    }

    protected virtual void SetItem(int index, T item)
    {
        Items[index] = item;
    }

    bool ICollection<T>.IsReadOnly
    {
        get { return false; }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IList)Items).GetEnumerator();
    }

    bool ICollection.IsSynchronized
    {
        get { return true; }
    }

    object ICollection.SyncRoot
    {
        get { return _sync; }
    }

    void ICollection.CopyTo(Array array, int index)
    {
        lock (_sync)
        {
            ((IList)Items).CopyTo(array, index);
        }
    }

    object? IList.this[int index]
    {
        get
        {
            return this[index]!;
        }
        set
        {
            VerifyValueType(value);
            this[index] = (T)value!;
        }
    }

    bool IList.IsReadOnly
    {
        get { return false; }
    }

    bool IList.IsFixedSize
    {
        get { return false; }
    }

    int IList.Add(object? value)
    {
        VerifyValueType(value);

        lock (_sync)
        {
            Add((T)value!);
            return Count - 1;
        }
    }

    bool IList.Contains(object? value)
    {
        VerifyValueType(value);
        return Contains((T)value!);
    }

    int IList.IndexOf(object? value)
    {
        VerifyValueType(value);
        return IndexOf((T)value!);
    }

    void IList.Insert(int index, object? value)
    {
        VerifyValueType(value);
        Insert(index, (T)value!);
    }

    void IList.Remove(object? value)
    {
        VerifyValueType(value);
        Remove((T)value!);
    }

    private static void VerifyValueType(object? value)
    {
        if (value == null)
        {
            if (typeof(T).GetTypeInfo().IsValueType)
            {
                throw new ArgumentException("Null is not a valid value for value-typed collections.");
            }
        }
        else if (!(value is T))
        {
            throw new ArgumentException("Value is of the wrong type.", nameof(value));
        }
    }
}
