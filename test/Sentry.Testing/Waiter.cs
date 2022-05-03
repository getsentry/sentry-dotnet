namespace Sentry.Testing;

public sealed class Waiter<T> : IDisposable
{
    private readonly TaskCompletionSource<object> _taskCompletionSource = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Waiter(Action<EventHandler<T>> callback)
    {
        _cancellationTokenSource.Token.Register(() => _taskCompletionSource.SetCanceled());
        callback.Invoke((_, _) => _taskCompletionSource.SetResult(null));
    }

    public async Task WaitAsync(TimeSpan timeout)
    {
        _cancellationTokenSource.CancelAfter(timeout);
        await _taskCompletionSource.Task;
    }

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
