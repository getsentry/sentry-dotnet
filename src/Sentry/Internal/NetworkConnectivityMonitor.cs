namespace Sentry.Internal;

internal class NetworkConnectivityMonitor : IDisposable
{
    private readonly IPingHost _pingHost;
    private readonly Action _callbackWhenHostOnline;
    private readonly CancellationTokenSource _workerCts = new();
    private readonly Task _worker;
    internal int _delayInMilliseconds;
    private readonly int _maxDelayInMilliseconds;
    private readonly Func<int, int> _backoffFunction;

    public NetworkConnectivityMonitor(string hostToCheck, Action callbackWhenHostOnline,
        int initialDelayInMilliseconds = 500,
        int maxDelayInMilliseconds = 32_000, Func<int, int>? backoffFunction = null)
        : this(new PingHost(hostToCheck), callbackWhenHostOnline, initialDelayInMilliseconds, maxDelayInMilliseconds,
            backoffFunction)
    {
    }

    internal NetworkConnectivityMonitor(IPingHost pingHost, Action callbackWhenHostOnline,
        int initialDelayInMilliseconds = 500,
        int maxDelayInMilliseconds = 32_000, Func<int, int>? backoffFunction = null)
    {
        _pingHost = pingHost;
        _callbackWhenHostOnline = callbackWhenHostOnline;
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
            var checkResult = await IsNetworkAvailableAsync(_workerCts.Token).ConfigureAwait(false);
            if (checkResult)
            {
                _callbackWhenHostOnline();
                return;
            }
            if (_delayInMilliseconds < _maxDelayInMilliseconds)
            {
                _delayInMilliseconds = _backoffFunction(_delayInMilliseconds);
            }
        }
    }

    private async Task<bool> IsNetworkAvailableAsync(CancellationToken cancellationToken) => await
        _pingHost.IsAvailableAsync(cancellationToken).ConfigureAwait(false);

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
