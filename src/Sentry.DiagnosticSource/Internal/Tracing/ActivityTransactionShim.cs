// Alias required because Android TFMs of the core Sentry package otherwise see an ambiguous reference
// between System.Diagnostics.Activity (global using) and Android.App.Activity (Android implicit usings).
using Activity = System.Diagnostics.Activity;

namespace Sentry.Internal.Tracing;

/// <summary>
/// An <see cref="ITransactionTracer"/> facade over an <see cref="Activity"/>. See
/// <see cref="ActivitySpanShim"/> for the design principle. Created by <see cref="Create"/>, which
/// <c>Hub.StartTransaction</c> invokes (via <c>SentryOptions.ActivityShimFactory</c>) for Sentry-API
/// transactions when Activity-based tracing is enabled.
/// </summary>
internal sealed class ActivityTransactionShim : ActivitySpanShim, ITransactionTracer, IAutoTimeoutTracer
{
    private readonly ITransactionTracer _inner;

    private ActivityTransactionShim(Activity activity, ITransactionTracer inner, IHub hub)
        : base(activity, inner, hub)
    {
        _inner = inner;
    }

    /// <summary>
    /// Starts an Activity for a Sentry-API transaction and returns a shim wrapping it, or null when no
    /// activity listener is running (callers should fall back to the classic tracing path).
    /// </summary>
    /// <remarks>
    /// Uses CreateActivity + Start (rather than StartActivity) so that side-channel values the
    /// <see cref="SentryActivityProcessor"/> needs at OnStart time - the custom sampling context, an inbound
    /// dynamic sampling context and the transaction name - are all in place before OnStart runs sampling.
    /// </remarks>
    public static ITransactionTracer? Create(
        IHub hub,
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext,
        DynamicSamplingContext? dynamicSamplingContext,
        TimeSpan? idleTimeout)
    {
        // Continue an inbound (remote) trace, propagating the upstream sampling decision.
        ActivityContext parentContext = default;
        if (context.ParentSpanId is { } parentSpanId && parentSpanId != default(SpanId))
        {
            var flags = context.IsParentSampled == true ? ActivityTraceFlags.Recorded : ActivityTraceFlags.None;
            parentContext = new ActivityContext(
                context.TraceId.AsActivityTraceId(), parentSpanId.AsActivitySpanId(), flags, isRemote: true);
        }

        // Sentry semantics: StartTransaction begins a new trace (unless explicitly continuing a remote one).
        // Detach from any ambient Activity so the new activity does not become an implicit child of it -
        // e.g. a second UI click must be an independent transaction, not a child of the (still idling) first.
        var ambientActivity = Activity.Current;
        Activity.Current = null;

        var activity = SentryActivitySources.Shim.CreateActivity(
            context.Operation, ActivityKind.Internal, parentContext);
        if (activity is null)
        {
            // No listener is subscribed to the Sentry ActivitySource.
            Activity.Current = ambientActivity;
            return null;
        }

        activity.DisplayName = context.Name;
        activity.SetFused(ShimKeys.CustomSamplingContext, customSamplingContext);
        if (idleTimeout is not null)
        {
            activity.SetFused(ShimKeys.IdleTimeout, idleTimeout);
        }
        if (dynamicSamplingContext is not null)
        {
            activity.SetFused(dynamicSamplingContext);
        }

        activity.Start();

        if (activity.GetFused<ISpan>() is not ITransactionTracer inner)
        {
            // The listener saw the activity but did not map it (e.g. the hub was disabled mid-flight).
            activity.Dispose();
            Activity.Current = ambientActivity;
            return null;
        }

        var shim = new ActivityTransactionShim(activity, inner, hub);

        // Note: out-of-band tracer finishes (idle timer) stop the backing Activity via the OnFinished
        // hook the processor installs on every activity-derived transaction.

        // The processor put the shadow tracer on the scope; replace it with the shim so that
        // integrations calling hub.GetSpan()/scope.Transaction route child spans through Activities.
        hub.ConfigureScope(static (scope, s) => scope.Transaction = s, shim);
        return shim;
    }

    // ---- ITransactionTracer ----

    public string Name
    {
        get => _inner.Name;
        set
        {
            // The DisplayName must be kept in sync because the processor derives the transaction name
            // from the Activity in OnEnd (which would otherwise clobber a rename back to the old name).
            Activity.DisplayName = value;
            _inner.Name = value;
        }
    }

    public TransactionNameSource NameSource => _inner.NameSource;

    public bool? IsParentSampled
    {
        get => _inner.IsParentSampled;
        set => _inner.IsParentSampled = value;
    }

    public IReadOnlyCollection<ISpan> Spans => _inner.Spans;

    public ISpan? GetLastActiveSpan()
    {
        if (_inner.GetLastActiveSpan() is not { } last)
        {
            return null;
        }
        // Every span the processor creates has its Activity fused on; wrap it so that callers
        // (e.g. integrations creating children of the "current" span) route through Activities.
        return last.GetFused<Activity>() is { } activity
            ? new ActivitySpanShim(activity, last, Hub)
            : last;
    }

    // ---- IEventLike (delegated) ----

    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _inner.Breadcrumbs;
    public void AddBreadcrumb(Breadcrumb breadcrumb) => _inner.AddBreadcrumb(breadcrumb);

    public string? Distribution
    {
        get => _inner.Distribution;
        set => _inner.Distribution = value;
    }

    public SentryLevel? Level
    {
        get => _inner.Level;
        set => _inner.Level = value;
    }

    public SentryRequest Request
    {
        get => _inner.Request;
        set => _inner.Request = value;
    }

    public SentryContexts Contexts
    {
        get => _inner.Contexts;
        set => _inner.Contexts = value;
    }

    public SentryUser User
    {
        get => _inner.User;
        set => _inner.User = value;
    }

    public string? Release
    {
        get => _inner.Release;
        set => _inner.Release = value;
    }

    public string? Environment
    {
        get => _inner.Environment;
        set => _inner.Environment = value;
    }

    public string? TransactionName
    {
        get => _inner.TransactionName;
        set => _inner.TransactionName = value;
    }

    public SdkVersion Sdk => _inner.Sdk;

    void IAutoTimeoutTracer.ResetIdleTimeout() => (_inner as IAutoTimeoutTracer)?.ResetIdleTimeout();

    public string? Platform
    {
        get => _inner.Platform;
        set => _inner.Platform = value;
    }

    public IReadOnlyList<string> Fingerprint
    {
        get => _inner.Fingerprint;
        set => _inner.Fingerprint = value;
    }
}
