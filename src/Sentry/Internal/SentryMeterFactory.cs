#if NET8_0_OR_GREATER
using System.Diagnostics.Metrics;

namespace Sentry.Internal;

/// <summary>
/// This is a simplified MeterFactory that only caches meters and does not support scoping. It also assumes there will
/// be no dimensionality in the meters. The main reason we have this is that that we can't rely on Dependency injection
/// so we can't get a DefaultMeterFactory from DI.
/// </summary>
internal class SentryMeterFactory : IMeterFactory
{
    private static readonly Lazy<SentryMeterFactory> LazyInstance = new();
    public static SentryMeterFactory Instance => LazyInstance.Value;

    private readonly Dictionary<string, Meter> _cachedMeters = new();
    private bool _disposed;

    public Meter Create(MeterOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (options.Scope is not null)
        {
            throw new ArgumentException("The SentryMeterFactory does not support scopes");
        }

        if (options.Tags != null && options.Tags.Any())
        {
            throw new ArgumentException("The SentryMeterFactory does not support tags");
        }

        Debug.Assert(options.Name is not null);

        lock (_cachedMeters)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SentryMeterFactory));
            }

            if (_cachedMeters.TryGetValue(options.Name, out var meter))
            {
                return meter;
            }

            meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
            _cachedMeters.Add(options.Name, meter);
            return meter;
        }
    }

    public void Dispose()
    {
        lock (_cachedMeters)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            foreach (var meter in _cachedMeters.Values)
            {
                meter.Dispose();
            }

            _cachedMeters.Clear();
        }
    }
}
#endif
