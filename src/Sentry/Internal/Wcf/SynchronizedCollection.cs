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
internal class SynchronizedCollection<T>
{
    private readonly object _sync = new();

    public IReadOnlyCollection<T> ToReadOnlyCollection()
    {
        lock (_sync)
        {
            // This will be pig slow but ensures that the collection is thread-safe for IEnumerable operations.
            // This is just to run an experiment - DO NOT MERGE THIS INTO THE MAIN BRANCH!!!
            return new ReadOnlyCollection<T>(Items.ToArray());
        }
    }

    public int Count
    {
        get { lock (_sync) { return Items.Count; } }
    }

    private List<T> Items { get; } = [];

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

                Items[index] = value;
            }
        }
    }

    public void Add(T item)
    {
        lock (_sync)
        {
            var index = Items.Count;
            Items.Insert(index, item);
        }
    }

    public void Clear()
    {
        lock (_sync)
        {
            Items.Clear();
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

            Items.Insert(index, item);
        }
    }

    private int InternalIndexOf(T item)
    {
        var count = Items.Count;

        for (var i = 0; i < count; i++)
        {
            if (Equals(Items[i], item))
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
            var index = InternalIndexOf(item);
            if (index < 0)
            {
                return false;
            }

            Items.RemoveAt(index);
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

            Items.RemoveAt(index);
        }
    }
}
