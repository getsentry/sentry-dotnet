namespace Sentry.Internal.Tracing;

#if !NET6_0_OR_GREATER
using System.Diagnostics;
#endif

/// <summary>
/// Wraps the functionality that we use from Activity in an interface so that we can
/// access this from our integrations without taking a hard dependency on
/// System.Diagnostics.Activity (which is only available in .NET 5.0 and later)
/// </summary>
internal class ActivityWrapper(System.Diagnostics.Activity activity) : ISentrySpan
{
    public void SetAttribute(string key, object value) => activity.SetTag(key, value);

    public void AddEvent(string message) => activity.AddEvent(new ActivityEvent(message));

    public void SetStatus(SpanStatus status, string? description = default)
    {
        if (status == SpanStatus.Ok)
        {
            activity.SetStatus(ActivityStatusCode.Ok);
            return;
        }
        var errorMessage = description ?? status.ToString();
        activity.SetStatus(ActivityStatusCode.Error, errorMessage);
    }

    public void Stop() => activity.Stop();

    public void Dispose() => activity.Dispose();
}
