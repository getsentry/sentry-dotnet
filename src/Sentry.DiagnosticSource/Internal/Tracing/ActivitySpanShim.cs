using Sentry.Protocol;

// Alias required because Android TFMs of the core Sentry package otherwise see an ambiguous reference
// between System.Diagnostics.Activity (global using) and Android.App.Activity (Android implicit usings).
using Activity = System.Diagnostics.Activity;

namespace Sentry.Internal.Tracing;

/// <summary>
/// An <see cref="ISpan"/> facade over an <see cref="Activity"/>.
/// </summary>
/// <remarks>
/// Design principle: <b>lifecycle, timing and parenting go through the Activity</b> (so the Activity is the
/// single source of truth for the span tree, and pure-OTel instrumentation interleaves naturally), while
/// <b>rich Sentry-only state delegates directly to the shadow tracer</b> that
/// <see cref="SentryActivityProcessor"/> created for the Activity (so nothing is lost squeezing Sentry
/// concepts through Activity tags). The only Sentry-native concepts that must survive the Activity lifecycle
/// boundary (an explicit <see cref="SpanStatus"/> passed to Finish) travel as side-channel values fused onto
/// the Activity, where the processor picks them up in OnEnd.
/// </remarks>
internal class ActivitySpanShim : ISpan
{
    protected internal Activity Activity { get; }
    private readonly ISpan _inner;
    protected readonly IHub Hub;

    public ActivitySpanShim(Activity activity, ISpan inner, IHub hub)
    {
        Activity = activity;
        _inner = inner;
        Hub = hub;
    }

    internal ISpan Inner => _inner;

    // ---- Lifecycle: routed through the Activity ----

    public ISpan StartChild(string operation)
    {
        // Parent explicitly to this shim's Activity (not Activity.Current) to preserve Sentry's
        // StartChild-on-a-specific-span semantics.
        var childActivity = SentryActivitySources.Shim.StartActivity(
            operation, ActivityKind.Internal, Activity.Context);

        if (childActivity?.GetFused<ISpan>() is { } childSpan)
        {
            return new ActivitySpanShim(childActivity, childSpan, Hub);
        }

        // No listener is running (or the activity was suppressed) - fall back to the classic path.
        childActivity?.Dispose();
        return _inner.StartChild(operation);
    }

    public void Finish()
    {
        // An explicit status set via the property (rather than Finish(status)) must survive OnEnd,
        // which would otherwise derive the status from the Activity.
        if (_inner.Status is { } status)
        {
            FuseStatus(status);
        }
        StopActivity();
    }

    public void Finish(SpanStatus status)
    {
        FuseStatus(status);
        Activity.SetStatus(status == SpanStatus.Ok ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        StopActivity();
    }

    public void Finish(Exception exception, SpanStatus status)
    {
        Hub.BindException(exception, _inner);
        Finish(status);
    }

    public void Finish(Exception exception) => Finish(exception, SpanStatusConverter.FromException(exception));

    public void Dispose() => Finish();

    private void FuseStatus(SpanStatus status) => Activity.SetFused(ShimKeys.SpanStatus, status);

    private void StopActivity()
    {
        if (!Activity.IsStopped)
        {
            Activity.Stop();
        }
    }

    // ---- Rich state: delegated to the shadow tracer ----

    public string? Description
    {
        get => _inner.Description;
        set => _inner.Description = value;
    }

    public string Operation
    {
        get => _inner.Operation;
        set => _inner.Operation = value;
    }

    public SpanStatus? Status
    {
        get => _inner.Status;
        set => _inner.Status = value;
    }

    public SpanId SpanId => _inner.SpanId;
    public SpanId? ParentSpanId => _inner.ParentSpanId;
    public SentryId TraceId => _inner.TraceId;
    public string? Origin => _inner.Origin;
    public bool? IsSampled => _inner.IsSampled;

    public DateTimeOffset StartTimestamp => _inner.StartTimestamp;
    public DateTimeOffset? EndTimestamp => _inner.EndTimestamp;
    public bool IsFinished => _inner.IsFinished;
    public SentryTraceHeader GetTraceHeader() => _inner.GetTraceHeader();

    public IReadOnlyDictionary<string, Measurement> Measurements => _inner.Measurements;
    public void SetMeasurement(string name, Measurement measurement) => _inner.SetMeasurement(name, measurement);

    public IReadOnlyDictionary<string, object?> Data => _inner.Data;
    public void SetData(string key, object? value) => _inner.SetData(key, value);

    public IReadOnlyDictionary<string, string> Tags => _inner.Tags;
    public void SetTag(string key, string value) => _inner.SetTag(key, value);
    public void UnsetTag(string key) => _inner.UnsetTag(key);

    public IReadOnlyDictionary<string, object?> Extra => _inner.Extra;
    public void SetExtra(string key, object? value) => _inner.SetExtra(key, value);
}
