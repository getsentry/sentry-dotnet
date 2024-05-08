namespace Sentry.Internal;

/// <summary>
/// This class is purely for testing purposes. It's been hacked together in a short amount of time. Performance is no
/// doubt terrible and it should in no way be used in production code. It does confirm we have a memory issue with the
/// <see cref="ConcurrentQueue{T}"/> class however. See https://github.com/getsentry/sentry-dotnet/issues/2516
/// </summary>
internal class ConcurrentQueueLite<T>
{
    private readonly List<T> _queue = new();
    private int _listCounter = 0;

    public void Enqueue(T item)
    {
        lock (_queue)
        {
            _queue.Add(item);
            _listCounter++;
        }
    }
    public bool TryDequeue([NotNullWhen(true)] out T? item)
    {
        item = default;
        lock (_queue)
        {
            if (_listCounter > 0)
            {
                item = _queue[0];
                _queue.RemoveAt(0);
                _listCounter--;
            }
        }
        return item != null;
    }

    public int Count => _listCounter;

    public bool IsEmpty => _listCounter == 0;

    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
            _listCounter = 0;
        }
    }

    public bool TryPeek([NotNullWhen(true)] out T? item)
    {
        item = default;
        lock (_queue)
        {
            if (_listCounter > 0)
            {
                item = _queue[0];
            }
        }
        return item != null;
    }
}
