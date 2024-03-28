namespace Sentry.Internal.Tracing;

/// <summary>
/// Wraps the functionality that we use from ActivitySource in an interface so that we
/// can access this from our integrations without taking a hard dependency on
/// System.Diagnostics.ActivitySource (which is only available in .NET 5.0 and later)
/// </summary>
internal class ActivityTracer(string name, string? version = "") : ITracer
{
    private readonly ActivitySource _activitySource = new(name, version);

    public ITraceSpan? StartSpan(string operationName) =>
        _activitySource.StartActivity(operationName) is { } activity ? new ActivityTraceSpan(activity) : null;

    public ITraceSpan? CurrentSpan => System.Diagnostics.Activity.Current == null
        ? null
        : new ActivityTraceSpan(System.Diagnostics.Activity.Current);
}
