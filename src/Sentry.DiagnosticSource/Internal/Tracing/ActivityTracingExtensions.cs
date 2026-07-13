// Alias required because Android TFMs of the core Sentry package otherwise see an ambiguous reference
// between System.Diagnostics.Activity (global using) and Android.App.Activity (Android implicit usings).
using Activity = System.Diagnostics.Activity;

namespace Sentry.Internal.Tracing;

/// <summary>
/// The API surface Sentry's own integrations use when instrumenting directly with Activities (i.e. without
/// the <see cref="ActivityTransactionShim"/>). These helpers cover the Sentry-specific semantics that raw
/// <see cref="ActivitySource"/> calls don't express: starting an independent root (transaction), idle
/// timeouts, rich span statuses.
/// </summary>
internal static class ActivityTracingExtensions
{
    /// <summary>
    /// Starts an Activity that is an independent trace root - the Sentry-transaction equivalent.
    /// Detaches from any ambient Activity first (Sentry semantics: a transaction begins a new trace),
    /// and optionally carries an idle timeout, which the <see cref="SentryActivityProcessor"/> hands to
    /// the shadow tracer so the transaction auto-finishes (or is discarded when trivial) after inactivity.
    /// </summary>
    public static Activity? StartRootActivity(
        this ActivitySource source, string operation, string name, TimeSpan? idleTimeout = null)
    {
        Activity.Current = null;

        var activity = source.CreateActivity(operation, ActivityKind.Internal);
        if (activity is null)
        {
            // No listener is subscribed to this source.
            return null;
        }

        activity.DisplayName = name;
        if (idleTimeout is not null)
        {
            activity.SetFused(ShimKeys.IdleTimeout, idleTimeout);
        }

        activity.Start();
        return activity;
    }

    /// <summary>
    /// Resets the idle timeout of a root Activity started with an idle timeout (see
    /// <see cref="StartRootActivity"/>), e.g. on user interaction.
    /// </summary>
    public static void ResetIdleTimeout(this Activity activity) =>
        (activity.GetFused<ISpan>() as IAutoTimeoutTracer)?.ResetIdleTimeout();

    /// <summary>
    /// Stops the Activity with a rich Sentry <see cref="SpanStatus"/> - statuses like Cancelled have no
    /// <see cref="ActivityStatusCode"/> equivalent and travel via the fused side-channel instead.
    /// </summary>
    public static void Stop(this Activity activity, SpanStatus status)
    {
        activity.SetFused(ShimKeys.SpanStatus, status);
        activity.SetStatus(status == SpanStatus.Ok ? ActivityStatusCode.Ok : ActivityStatusCode.Error);
        activity.Stop();
    }
}
