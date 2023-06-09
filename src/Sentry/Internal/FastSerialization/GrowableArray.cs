namespace Sentry.Internal;

/// <summary>
/// A GrowableArray that can be used as a key in a Dictionary.
/// Note: it must be Seal()-ed before used as a key and can't be changed afterwards.
/// </summary>
internal struct HashableGrowableArray<T> : IReadOnlyList<T>, IEquatable<HashableGrowableArray<T>> where T : notnull
{
    private GrowableArray<T> _items;
    private int _hashCode = 0;
    private bool _sealed = false;

    public HashableGrowableArray(int capacity)
    {
        _items = new GrowableArray<T>(capacity);
    }

    public T this[int index]
    {
        get
        {
            return _items[index];
        }
        set
        {
            Debug.Assert(!_sealed);
            _items[index] = value;
        }
    }

    public int Count => _items.Count;

    /// <summary>
    /// Seal this array so that it cannot be changed anymore and can be hashed.
    /// </summary>
    public void Seal()
    {
        Debug.Assert(!_sealed);
        _sealed = true;
        foreach (var item in _items)
        {
            _hashCode = HashCode.Combine(_hashCode, item.GetHashCode());
        }
    }

    /// <summary>
    /// Trims the size of the array so that no more than 'maxWaste' slots are wasted.
    /// You can call this even on Seal()'ed array because it doesn't affect the content and thus the hash code.
    /// </summary>
    public void Trim(int maxWaste)
    {
        _items.Trim(maxWaste);
    }

    public void Add(T item)
    {
        Debug.Assert(!_sealed);
        _items.Add(item);
    }

    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        Debug.Assert(_sealed);
        return _hashCode;
    }

    public bool Equals(HashableGrowableArray<T> other)
    {
        Debug.Assert(_sealed);
        return _hashCode == other._hashCode && this.SequenceEqual(other);
    }

    public override bool Equals(object? obj) => obj is HashableGrowableArray<T> other && Equals(other);

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
}

/// <summary>
/// A cheap version of List(T). The idea is to make it as cheap as if you did it 'by hand' using an array and
/// an int which represents the logical charCount. It is a struct to avoid an extra pointer dereference, so this
/// is really meant to be embedded in other structures.
/// </summary>
/// <remarks>
/// Adapted to null-safety from the original version licensed under MIT and located at:
/// https://github.com/microsoft/perfview/blob/050c303943e74ff51ce584b2717e578d96684e85/src/FastSerialization/GrowableArray.cs
/// </remarks>
internal struct GrowableArray<T> : IReadOnlyList<T>
{
    /// <summary>
    /// Create a growable array with the given initial size it will grow as needed.
    /// </summary>
    /// <param name="initialSize"></param>
    public GrowableArray(int initialSize)
    {
        array = new T[initialSize];
        arrayLength = 0;
    }

    /// <summary>
    /// Fetch the element at the given index which must be lower than `Count`.
    /// </summary>
    public T this[int index]
    {
        get
        {
            Debug.Assert((uint)index < (uint)arrayLength);
            return array[index];
        }
        set
        {
            Debug.Assert((uint)index < (uint)arrayLength);
            array[index] = value;
        }
    }

    /// <summary>
    /// The number of elements in the array
    /// </summary>
    public int Count
    {
        get
        {
            return arrayLength;
        }
    }

    public void Reserve(int size)
    {
        if (arrayLength < size)
        {
            Realloc(size);
        }
    }

    /// <summary>
    /// Remove all elements in the array.
    /// </summary>
    public void Clear()
    {
        arrayLength = 0;
    }

    /// <summary>
    /// Add an item at the end of the array, growing as necessary.
    /// </summary>
    /// <param name="item"></param>
    public void Add(T item)
    {
        if (arrayLength >= array.Length)
        {
            Realloc(0);
        }

        array[arrayLength++] = item;
    }

    /// <summary>
    /// Add all items 'items' to the end of the array, growing as necessary.
    /// </summary>
    /// <param name="items"></param>
    public void AddRange(IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            Add(item);
        }
    }
    /// <summary>
    /// Insert 'item' directly at 'index', shifting all items >= index up.  'index' can be code:Count in
    /// which case the item is appended to the end.  Larger indexes are not allowed.
    /// </summary>
    public void Insert(int index, T item)
    {
        if ((uint)index > (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        if (arrayLength >= array.Length)
        {
            Realloc(0);
        }

        // Shift everything up to make room.
        for (int idx = arrayLength; index < idx; --idx)
        {
            array[idx] = array[idx - 1];
        }

        // insert the element
        array[index] = item;
        arrayLength++;
    }

    /// <summary>
    /// Remove 'count' elements starting at 'index'
    /// </summary>
    public void RemoveRange(int index, int count)
    {
        if (count == 0)
        {
            return;
        }

        if (count < 0)
        {
            throw new ArgumentException("count can't be negative");
        }

        if ((uint)index >= (uint)arrayLength)
        {
            throw new IndexOutOfRangeException();
        }

        Debug.Assert(index + count <= arrayLength);     // If you violate this it does not hurt

        // Shift everything down.
        for (int endIndex = index + count; endIndex < arrayLength; endIndex++)
        {
            array[index++] = array[endIndex];
        }

        arrayLength = index;
    }

    // Support for stack-like operations
    /// <summary>
    /// Returns true if there are no elements in the array.
    /// </summary>
    public bool Empty { get { return arrayLength == 0; } }

    /// <summary>
    /// Trims the size of the array so that no more than 'maxWaste' slots are wasted.   Useful when
    /// you know that the array has stopped growing.
    /// </summary>
    public void Trim(int maxWaste)
    {
        if (array.Length > arrayLength + maxWaste)
        {
            if (arrayLength == 0)
            {
                array = new T[0];
            }
            else
            {
                T[] newArray = new T[arrayLength];
                Array.Copy(array, newArray, arrayLength);
                array = newArray;
            }
        }
    }

    /// <summary>
    /// Returns true if the Growable array was initialized by the default constructor
    /// which has no capacity (and thus will cause growth on the first addition).
    /// This method allows you to lazily set the compacity of your GrowableArray by
    /// testing if it is of EmtpyCapacity, and if so set it to some useful capacity.
    /// This avoids unnecessary reallocs to get to a reasonable capacity.
    /// </summary>
    public bool EmptyCapacity { get { return array == null; } }

    /// <summary>
    /// A string representing the array. Only intended for debugging.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("GrowableArray(Count=").Append(Count).Append(", [").AppendLine();
        for (int i = 0; i < Count; i++)
        {
            sb.Append("  ").Append(this[i]?.ToString()).AppendLine();
        }

        sb.Append("  ])");
        return sb.ToString();
    }

    /// <summary>
    /// Executes 'func' for each element in the GrowableArray and returns a GrowableArray
    /// for the result.
    /// </summary>
    public GrowableArray<T1> Foreach<T1>(Func<T, T1> func)
    {
        var ret = new GrowableArray<T1>(Count);

        for (int i = 0; i < Count; i++)
        {
            ret[i] = func(array[i]);
        }

        return ret;
    }

    /// <summary>
    /// Perform a linear search starting at 'startIndex'.  If found return true and the index in 'index'.
    /// It is legal that 'startIndex' is greater than the charCount, in which case, the search returns false
    /// immediately.   This allows a nice loop to find all items matching a pattern.
    /// </summary>
    public bool Search<Key>(Key key, int startIndex, Func<Key, T, int> compare, ref int index)
    {
        for (int i = startIndex; i < arrayLength; i++)
        {
            if (compare(key, array[i]) == 0)
            {
                index = i;
                return true;
            }
        }
        return false;
    }
    /// <summary>
    /// Returns the underlying array.  Should not be used most of the time!
    /// </summary>
    public T[] UnderlyingArray { get { return array; } }

    #region private
    private void Realloc(int minSize)
    {
        long expandSize = (long)array.Length * 3 / 2 + 8;
        if (expandSize > int.MaxValue)
        {
            if (array.Length == int.MaxValue)
                throw new NotSupportedException("Array cannot have more than int.MaxValue elements.");

            expandSize = int.MaxValue;
        }

        if (minSize < expandSize)
        {
            minSize = (int)expandSize;
        }

        Array.Resize(ref array, minSize);
    }

    private T[] array;
    private int arrayLength;
    #endregion

    // Implementation of IEnumerable.
    IEnumerator IEnumerable.GetEnumerator() => new GrowableArrayEnumerator(this);

    // Implementation of IEnumerable<T>.
    public IEnumerator<T> GetEnumerator() => new GrowableArrayEnumerator(this);

    internal void Add(object value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// IEnumerator implementation.
    /// </summary>
    public struct GrowableArrayEnumerator : IEnumerator<T>
    {
        object IEnumerator.Current => Current!;

        public T Current
        {
            get
            {
                if (cur < 0 || cur >= end)
                {
                    throw new InvalidOperationException();
                }
                return array[cur]!;
            }
        }

        public bool MoveNext()
        {
            cur++;
            return cur < end;
        }

        public void Reset()
        {
            cur = -1;
        }

        public void Dispose() { }

        #region private
        internal GrowableArrayEnumerator(GrowableArray<T> growableArray)
        {
            cur = -1;
            end = growableArray.arrayLength;
            array = growableArray.array;
        }

        private int cur;
        private int end;
        private T[] array;
        #endregion
    }
}
