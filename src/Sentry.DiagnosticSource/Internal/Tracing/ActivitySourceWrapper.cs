namespace Sentry.Internal.Tracing;

/// <summary>
/// Wraps the functionality that we use from ActivitySource in an interface so that we
/// can access this from our integrations without taking a hard dependency on
/// System.Diagnostics.ActivitySource (which is only available in .NET 5.0 and later)
/// </summary>
internal class ActivitySourceWrapper(string name, string? version = "") : ISentryTracer
{
    private readonly ActivitySource _activitySource = new(name, version);

    public ISentrySpan? StartSpan(string operationName) =>
        _activitySource.StartActivity(operationName) is { } activity ? new ActivityWrapper(activity) : null;

    public ISentrySpan? CurrentSpan => System.Diagnostics.Activity.Current == null
        ? null
        : new ActivityWrapper(System.Diagnostics.Activity.Current);
}
