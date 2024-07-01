namespace Sentry.Internal;

internal class ProgressiveBackoff : IDisposable
{
    private readonly Func<CancellationToken, Task<bool>> _check;
    private readonly CancellationTokenSource _workerCts = new();
    private readonly Task _worker;
    internal int _delayInMilliseconds;
    private readonly int _maxDelayInMilliseconds;
    private readonly Func<int, int> _backoffFunction;

    public ProgressiveBackoff(Func<CancellationToken, Task<bool>> check, int initialDelayInMilliseconds = 500,
        int maxDelayInMilliseconds = 32_000, Func<int, int>? backoffFunction = null)
    {
        _check = check;
        _delayInMilliseconds = initialDelayInMilliseconds;
        _maxDelayInMilliseconds = maxDelayInMilliseconds;
        _backoffFunction = backoffFunction ?? (x => x * 2);
        _worker = Task.Run(DoCheckAsync);
    }

    private async Task DoCheckAsync()
    {
        while (!_workerCts.IsCancellationRequested)
        {
            await Task.Delay(_delayInMilliseconds, _workerCts.Token).ConfigureAwait(false);
            var checkResult = await _check(_workerCts.Token).ConfigureAwait(false);
            if (checkResult)
            {
                return;
            }
            if (_delayInMilliseconds < _maxDelayInMilliseconds)
            {
                _delayInMilliseconds = _backoffFunction(_delayInMilliseconds);
            }
        }
    }

    public void Dispose()
    {
        _workerCts.Cancel();
        try
        {
            _worker.Wait();
        }
        catch
        {
            // ignored
        }
        _workerCts.Dispose();
        _worker.Dispose();
    }
}
