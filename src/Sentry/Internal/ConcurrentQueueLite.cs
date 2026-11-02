namespace Sentry.Internal;

/// <summary>
/// A minimal replacement for <see cref="ConcurrentQueue{T}"/>
///
/// We're using this due to a memory leak that happens when using ConcurrentQueue in the BackgroundWorker.
/// See https://github.com/getsentry/sentry-dotnet/issues/2516
/// </summary>
internal class ConcurrentQueueLite<T> : IReadOnlyCollection<T>
{
    private readonly List<T> _queue = new();

    public void Enqueue(T item)
    {
        lock (_queue)
        {
            _queue.Add(item);
        }
    }

    /// <summary>
    /// Appends all the items in <paramref name="source"/> to this queue.
    /// Intended for populating a queue that is not yet shared with other threads.
    /// </summary>
    public void EnqueueRange(ConcurrentQueueLite<T> source)
    {
        lock (source._queue)
        {
            if (source._queue.Count == 0)
            {
                return;
            }

            lock (_queue)
            {
                _queue.AddRange(source._queue);
            }
        }
    }

    public bool TryDequeue([NotNullWhen(true)] out T? item)
    {
        lock (_queue)
        {
            if (_queue.Count > 0)
            {
                item = _queue[0]!;
                _queue.RemoveAt(0);
                return true;
            }
        }
        item = default;
        return false;
    }

    public int Count
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }

    public bool IsEmpty => Count == 0;

    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
        }
    }

    public bool TryPeek([NotNullWhen(true)] out T? item)
    {
        lock (_queue)
        {
            if (_queue.Count > 0)
            {
                item = _queue[0]!;
                return true;
            }
        }
        item = default;
        return false;
    }

    public T[] ToArray()
    {
        lock (_queue)
        {
            return _queue.ToArray();
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        // Return a snapshot to avoid holding the lock during iteration
        // and to prevent InvalidOperationException if the collection is modified.
        T[] snapshot;
        lock (_queue)
        {
            snapshot = _queue.ToArray();
        }
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
