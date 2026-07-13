using Sentry.Internal.Tracing;

namespace Sentry.Maui.Internal;

/// <summary>
/// Spike (Q2): the direct-Activity equivalent of the tracing concerns in <see cref="MauiEventsBinder"/>
/// (StartUiTransaction / StartNavigationSpan), showing what that code becomes once the shim methods are
/// replaced with Activity instrumentation. Compare side by side:
/// <code>
///   // shim (today):                                      // direct (this class):
///   internalHub.StartTransaction(context, idleTimeout)    Source.StartRootActivity(op, name, idleTimeout)
///   autoTimeoutTracer.ResetIdleTimeout()                  CurrentUiActivity.ResetIdleTimeout()
///   parentSpan.StartChild("ui.load", name)                Source.StartActivity("ui.load") + DisplayName
///   navSpan.Finish(SpanStatus.Ok)                         CurrentNavActivity.Stop(SpanStatus.Ok)
/// </code>
/// The idle mechanics (timer, pause-while-children-active, discard-if-empty, end-time trimming) are
/// unchanged - they run on the shadow tracer, driven by the idle timeout fused onto the root Activity.
/// </summary>
internal class MauiUiActivityTracing
{
    // Note: ShouldListenTo callbacks fire while this static initializer is still running (the runtime
    // announces new ActivitySources to registered listeners inside the ActivitySource constructor), so
    // listener filters must compare by name (the const below) rather than by reference to this field.
    internal const string SourceName = "Sentry.Maui";
    internal static readonly ActivitySource Source = new(SourceName);

    private readonly TimeSpan _idleTimeout;

    internal Activity? CurrentUiActivity { get; private set; }
    internal Activity? CurrentNavActivity { get; private set; }

    public MauiUiActivityTracing(TimeSpan idleTimeout)
    {
        _idleTimeout = idleTimeout;
    }

    public void StartUiTransaction(string name)
    {
        // Each UI interaction is a separate transaction... we don't want separate button clicks grouped
        // as a single one. Any in-flight navigation span belongs to the previous interaction.
        if (CurrentNavActivity is { IsStopped: false } navActivity)
        {
            navActivity.Stop(SpanStatus.Cancelled);
            CurrentNavActivity = null;
        }

        // The previous UI transaction (if any) is left to idle out; StartRootActivity detaches from it,
        // so nothing new can parent under it from this flow.
        CurrentUiActivity = Source.StartRootActivity(
            MauiEventsBinder.UserInteractionClickOp, name, _idleTimeout);
    }

    public Activity? StartNavigationSpan(string name)
    {
        // Nav events only ever get captured as child spans.
        if (CurrentUiActivity is not { IsStopped: false } uiActivity)
        {
            return null;
        }

        uiActivity.ResetIdleTimeout();

        CurrentNavActivity?.Stop(SpanStatus.Ok);
        CurrentNavActivity = Source.StartActivity(
            "ui.load", ActivityKind.Internal, uiActivity.Context);
        if (CurrentNavActivity is not null)
        {
            CurrentNavActivity.DisplayName = name;
        }
        return CurrentNavActivity;
    }
}
