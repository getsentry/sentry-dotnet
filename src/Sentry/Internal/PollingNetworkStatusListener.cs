using Sentry.Extensibility;

namespace Sentry.Internal;

internal class PollingNetworkStatusListener : INetworkStatusListener
{
    private long _networkIsUnavailable = 0;

    private readonly SentryOptions? _options;
    private readonly IPing? _testPing;
    internal int _delayInMilliseconds;
    private readonly int _maxDelayInMilliseconds;
    private readonly Func<int, int> _backoffFunction;

    public PollingNetworkStatusListener(SentryOptions options, int initialDelayInMilliseconds = 500,
        int maxDelayInMilliseconds = 32_000, Func<int, int>? backoffFunction = null)
    {
        _options = options;
        _delayInMilliseconds = initialDelayInMilliseconds;
        _maxDelayInMilliseconds = maxDelayInMilliseconds;
        _backoffFunction = backoffFunction ?? (x => x * 2);
    }

    /// <summary>
    /// Overload for testing
    /// </summary>
    internal PollingNetworkStatusListener(IPing testPing, int initialDelayInMilliseconds = 500,
        int maxDelayInMilliseconds = 32_000, Func<int, int>? backoffFunction = null)
    {
        _testPing = testPing;
        _delayInMilliseconds = initialDelayInMilliseconds;
        _maxDelayInMilliseconds = maxDelayInMilliseconds;
        _backoffFunction = backoffFunction ?? (x => x * 2);
    }

    private Lazy<IPing> LazyPing => new(() =>
    {
        if (_testPing != null)
        {
            return _testPing;
        }
        var uri = new Uri(_options!.Dsn!);
        return new TcpPing(uri.DnsSafeHost, uri.Port);
    });
    private IPing Ping => LazyPing.Value;

    public bool Online {
        get => Interlocked.Read(ref _networkIsUnavailable) == 0;
        set => Interlocked.Exchange(ref _networkIsUnavailable, value ? 0 : 1);
    }
    public async Task WaitForNetworkOnlineAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_delayInMilliseconds, cancellationToken).ConfigureAwait(false);
            var checkResult = await Ping.IsAvailableAsync().ConfigureAwait(false);
            if (checkResult)
            {
                Online = true;
                return;
            }
            if (_delayInMilliseconds < _maxDelayInMilliseconds)
            {
                _delayInMilliseconds = _backoffFunction(_delayInMilliseconds);
            }
        }
    }
}
