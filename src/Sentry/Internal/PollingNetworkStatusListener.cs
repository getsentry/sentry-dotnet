using Sentry.Extensibility;

namespace Sentry.Internal;

internal class PollingNetworkStatusListener : INetworkStatusListener
{
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
        // If not running unit tests then _options will not be null and SDK init would fail without a Dsn
        var uri = new Uri(_options!.Dsn!);
        return new TcpPing(uri.DnsSafeHost, uri.Port);
    });
    private IPing Ping => LazyPing.Value;

    private volatile bool _online = true;
    public bool Online
    {
        get => _online;
        set => _online = value;
    }

    public async Task WaitForNetworkOnlineAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_delayInMilliseconds, cancellationToken).ConfigureAwait(false);
                var checkResult = await Ping.IsAvailableAsync(cancellationToken).ConfigureAwait(false);
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
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
