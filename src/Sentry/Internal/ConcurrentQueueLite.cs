namespace Sentry.Internal;

/// <summary>
/// A minimal replacement for <see cref="ConcurrentQueue{T}"/>
///
/// We're using this due to a memory leak that happens when using ConcurrentQueue in the BackgroundWorker.
/// See https://github.com/getsentry/sentry-dotnet/issues/2516
/// </summary>
internal class ConcurrentQueueLite<T>
{
    private readonly List<T> _queue = new();

    public void Enqueue(T item)
    {
        lock (_queue)
        {
            _queue.Add(item);
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
}
