namespace Sentry.Internal.Tracing;

/// <summary>
/// The concrete implementation of <see cref="ITraceProvider"/> that uses
/// <see cref="System.Diagnostics.ActivitySource"/> and <see cref="System.Diagnostics.Activity"/> from the
/// <see cref="System.Diagnostics"/> namespace to implement tracing.
/// </summary>
internal class ActivityTraceProvider : ITraceProvider
{
    private Lazy<ConcurrentDictionary<string, ActivityTracer>> _lazyActivitySources = new();
    private ConcurrentDictionary<string, ActivityTracer> _activitySources => _lazyActivitySources.Value;

    public ITracer GetTracer(string name, string? version = "")
        => _activitySources.GetOrAdd(name, new ActivityTracer(name, version));
}
