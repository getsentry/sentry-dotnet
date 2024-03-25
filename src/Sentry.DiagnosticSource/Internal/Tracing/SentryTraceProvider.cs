namespace Sentry.Internal.Tracing;

/// <summary>
/// The default concrete implementation of <see cref="ISentryTraceProvider"/> that uses
/// <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Activity"/> from the
/// <see cref="System.Diagnostics"/> namespace to implement tracing.
/// </summary>
internal class SentryTraceProvider : ISentryTraceProvider
{
    private Lazy<ConcurrentDictionary<string, ActivitySourceWrapper>> _lazyActivitySources = new();
    private ConcurrentDictionary<string, ActivitySourceWrapper> _activitySources => _lazyActivitySources.Value;

    public ISentryTracer GetTracer(string name, string? version = "")
        => _activitySources.GetOrAdd(name, new ActivitySourceWrapper(name, version));
}
