using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Extension methods for <see cref="IHub"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HubExtensions
{
    /// <summary>
    /// Starts a transaction.
    /// </summary>
    public static ITransactionTracer StartTransaction(this IHub hub, ITransactionContext context) =>
        hub.StartTransaction(context, new Dictionary<string, object?>());

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    public static ITransactionTracer StartTransaction(
        this IHub hub,
        string name,
        string operation) =>
        hub.StartTransaction(new TransactionContext(name, operation));

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    public static ITransactionTracer StartTransaction(
        this IHub hub,
        string name,
        string operation,
        string? description)
    {
        var transaction = hub.StartTransaction(name, operation);
        transaction.Description = description;

        return transaction;
    }

    /// <summary>
    /// Starts a transaction from the specified trace header.
    /// </summary>
    public static ITransactionTracer StartTransaction(
        this IHub hub,
        string name,
        string operation,
        SentryTraceHeader traceHeader) =>
        hub.StartTransaction(new TransactionContext(name, operation, traceHeader));

    /// <summary>
    /// Starts a span or transaction if a transaction is not already active on the scope
    /// </summary>
    public static ISpan StartSpan(this IHub hub, string operation, string description)
    {
        return hub.GetTransaction() is { } transaction
            ? transaction.StartChild(operation, description)
            : hub.StartTransaction(operation, description); // this is actually in the wrong order but changing it may break other things
    }

    /// <summary>
    /// Records a transaction that has already completed elsewhere — for example, spans measured on another
    /// machine or process and replayed through a proxy. Timing is supplied explicitly rather than measured
    /// live, so unlike <see cref="StartTransaction(IHub, string, string)"/> there is no stopwatch, idle timer,
    /// or sampling decision involved; the transaction is materialized and captured once when
    /// <paramref name="configure"/> returns.
    /// </summary>
    /// <param name="hub">The hub.</param>
    /// <param name="name">The transaction name.</param>
    /// <param name="operation">The transaction operation.</param>
    /// <param name="startTimestamp">When the transaction started.</param>
    /// <param name="duration">How long the transaction ran. Must not be negative.</param>
    /// <param name="traceId">Optional trace id to preserve from the originating system; generated when omitted.</param>
    /// <param name="spanId">Optional root span id to preserve; generated when omitted.</param>
    /// <param name="parentSpanId">Optional parent span id, when continuing a trace from another service.</param>
    /// <param name="configure">
    /// Optional callback to set metadata, configure the capture scope, and record the span tree via
    /// <see cref="ISpanRecorder.RecordSpan(string, DateTimeOffset, TimeSpan, SpanId?, Action{ISpanRecorder}?)"/>.
    /// </param>
    /// <returns>The event id of the captured transaction.</returns>
    public static SentryId RecordTransaction(
        this IHub hub,
        string name,
        string operation,
        DateTimeOffset startTimestamp,
        TimeSpan duration,
        SentryId? traceId = null,
        SpanId? spanId = null,
        SpanId? parentSpanId = null,
        Action<ITransactionRecorder>? configure = null)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Transaction duration cannot be negative.");
        }

        var context = new TransactionContext(
            name,
            operation,
            spanId: spanId,
            parentSpanId: parentSpanId,
            traceId: traceId,
            isSampled: true);

        var tracer = new TransactionTracer(hub, context)
        {
            StartTimestamp = startTimestamp,
            EndTimestamp = startTimestamp + duration,
        };
        tracer.Status ??= SpanStatus.Ok;

        // A recorded transaction represents work that happened elsewhere, so we capture it against a clean
        // scope rather than the current (live) one — this stops the current process's breadcrumbs/user/tags/
        // contexts from leaking onto it. The scope is exposed via ITransactionRecorder.ConfigureScope so the
        // caller can still set data that was captured alongside the original trace.
        var options = hub.GetSentryOptions();
        var scope = options is not null ? new Scope(options) : null;

        var recorder = new TransactionRecorder(tracer, scope);
        configure?.Invoke(recorder);

        var transaction = new SentryTransaction(tracer);
        if (scope is not null)
        {
            hub.CaptureTransaction(transaction, scope, null);
        }
        else
        {
            // No options available (e.g. a disabled hub); this is effectively a no-op capture.
            hub.CaptureTransaction(transaction);
        }

        return transaction.EventId;
    }

    /// <summary>
    /// Adds a breadcrumb to the current scope.
    /// </summary>
    /// <param name="hub">The Hub which holds the scope stack.</param>
    /// <param name="message">The message.</param>
    /// <param name="category">Category.</param>
    /// <param name="type">Breadcrumb type.</param>
    /// <param name="data">Additional data.</param>
    /// <param name="level">Breadcrumb level.</param>
    public static void AddBreadcrumb(
        this IHub hub,
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
    {
        // Not to throw on code that ignores nullability warnings.
        if (hub.IsNull())
        {
            return;
        }

        hub.AddBreadcrumb(
            null,
            message,
            category,
            type,
            data != null ? new Dictionary<string, string>(data) : null,
            level);
    }

    /// <summary>
    /// Adds a breadcrumb using a custom <see cref="ISystemClock"/> which allows better testability.
    /// </summary>
    /// <param name="hub">The Hub which holds the scope stack.</param>
    /// <param name="clock">The system clock.</param>
    /// <param name="message">The message.</param>
    /// <param name="category">Category.</param>
    /// <param name="type">Breadcrumb type.</param>
    /// <param name="data">Additional data.</param>
    /// <param name="level">Breadcrumb level.</param>
    /// <remarks>
    /// This method is to be used by integrations to allow testing.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void AddBreadcrumb(
        this IHub hub,
        ISystemClock? clock,
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
    {
        // Not to throw on code that ignores nullability warnings.
        if (hub.IsNull())
        {
            return;
        }

        var breadcrumb = new Breadcrumb(
            (clock ?? SystemClock.Clock).GetUtcNow(),
            message,
            type,
            data != null ? new Dictionary<string, string>(data) : null,
            category,
            level
        );

        hub.AddBreadcrumb(
            breadcrumb
            );
    }

    /// <summary>
    /// Adds a breadcrumb to the current scope.
    /// </summary>
    /// <param name="hub">The Hub which holds the scope stack.</param>
    /// <param name="breadcrumb">The breadcrumb to add</param>
    /// <param name="hint">An hint provided with the breadcrumb in the BeforeBreadcrumb callback</param>
    public static void AddBreadcrumb(
        this IHub hub,
        Breadcrumb breadcrumb,
        SentryHint? hint = null
        )
    {
        // Not to throw on code that ignores nullability warnings.
        if (hub.IsNull())
        {
            return;
        }

        hub.ConfigureScope(
            static (s, arg) => s.AddBreadcrumb(arg.breadcrumb, arg.hint ?? new SentryHint()),
            (breadcrumb, hint));
    }

    /// <summary>
    /// Pushes a new scope while locking it which stop new scope creation.
    /// </summary>
    public static IDisposable PushAndLockScope(this IHub hub) => new LockedScope(hub);

    /// <summary>
    /// Lock the scope so subsequent <see cref="ISentryScopeManager.PushScope"/> don't create new scopes.
    /// </summary>
    /// <remarks>
    /// This is useful to stop following scope creation by other integrations
    /// like Loggers which guarantee log messages are not lost.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void LockScope(this IHub hub) => hub.ConfigureScope(static s => s.Locked = true);

    /// <summary>
    /// Unlocks the current scope to allow subsequent calls to <see cref="ISentryScopeManager.PushScope"/> create new scopes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void UnlockScope(this IHub hub) => hub.ConfigureScope(static s => s.Locked = false);

    private sealed class LockedScope : IDisposable
    {
        private readonly IDisposable _scope;

        public LockedScope(IHub hub)
        {
            _scope = hub.PushScope();
            hub.LockScope();
        }

        public void Dispose() => _scope.Dispose();
    }

    internal static SentryId CaptureExceptionInternal(this IHub hub, Exception ex)
    {
        // integrations path = default to false, no override
        ex.Data[Mechanism.HandledKey] ??= false;
        return hub.CaptureEvent(new SentryEvent(ex));
    }

    /// <summary>
    /// Captures the exception with a configurable scope callback.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <param name="handled">Whether the exception was handled by the caller. Applies only when no handled flag
    /// is already set on the exception (e.g. by an SDK integration); an existing value wins. Defaults to <c>true</c>.</param>
    /// <returns>The Id of the event</returns>
    public static SentryId CaptureException(this IHub hub, Exception ex, Action<Scope> configureScope, bool handled = true)
    {
        if (!hub.IsEnabled)
        {
            return SentryId.Empty;
        }

        ex.Data[Mechanism.HandledKey] ??= handled;
        return hub.CaptureEvent(new SentryEvent(ex), configureScope);
    }

    /// <summary>
    /// Captures feedback from the user.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    /// <param name="feedback">The feedback to send to Sentry.</param>
    /// <param name="configureScope">Callback method to configure the scope.</param>
    /// <param name="hint">
    /// An optional hint providing high-level context for the source of the event, including attachments
    /// </param>
    /// <returns>
    /// A <see cref="SentryId"/> that will contain the Id of the new event (if successful) or
    /// <see cref="SentryId.Empty"/> otherwise
    /// </returns>
    public static SentryId CaptureFeedback(this IHub hub, SentryFeedback feedback, Action<Scope> configureScope, SentryHint? hint = null)
        => hub.CaptureFeedback(feedback, out _, configureScope, hint);

    /// <summary>
    /// Captures a message with a configurable scope callback.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <param name="level">The message level.</param>
    /// <returns>The Id of the event</returns>
    public static SentryId CaptureMessage(this IHub hub, string message, Action<Scope> configureScope,
        SentryLevel level = SentryLevel.Info)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new SentryId();
        }

        var sentryEvent = new SentryEvent
        {
            Message = message,
            Level = level
        };

        return hub.CaptureEvent(sentryEvent, configureScope);
    }

    internal static ITransactionTracer StartTransaction(
        this IHub hub,
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext,
        DynamicSamplingContext? dynamicSamplingContext) => hub switch
        {
            Hub fullHub => fullHub.StartTransaction(context, customSamplingContext, dynamicSamplingContext),
            HubAdapter adapter => adapter.StartTransaction(context, customSamplingContext, dynamicSamplingContext),
            _ => hub.StartTransaction(context, customSamplingContext)
        };

    internal static ITransactionTracer? GetTransaction(this IHub hub)
    {
        if (hub is Hub fullHub)
        {
            return fullHub.ScopeManager.GetCurrent().Key.Transaction;
        }

        ITransactionTracer? transaction = null;
        hub.ConfigureScope(scope => transaction = scope.Transaction);
        return transaction;
    }

    internal static ITransactionTracer? GetTransactionIfSampled(this IHub hub)
    {
        var transaction = hub.GetTransaction();
        return transaction?.IsSampled == true ? transaction : null;
    }

    internal static Scope? GetScope(this IHub hub)
    {
        if (hub is Hub fullHub)
        {
            return fullHub.ScopeManager.GetCurrent().Key;
        }

        Scope? current = null;
        hub.ConfigureScope(scope => current = scope);
        return current;
    }

    /// <summary>
    /// Get <paramref name="traceId"/> of either the currently active Span, or the current Scope.
    /// Get <paramref name="spanId"/> only if there is a currently active Span.
    /// </summary>
    /// <remarks>
    /// Intended for use by <see cref="SentryLog"/> and <see cref="SentryMetric{T}"/>.
    /// </remarks>
    internal static void GetTraceIdAndSpanId(this IHub hub, out SentryId traceId, out SpanId? spanId)
    {
        if (hub.GetSentryOptions()?.ExternalPropagationContext?.Snapshot() is { TraceId: not null } externalPropagationContext)
        {
            traceId = externalPropagationContext.TraceId.Value;
            spanId = externalPropagationContext.SpanId;
            return;
        }

        if (hub.GetSpan() is { } activeSpan)
        {
            traceId = activeSpan.TraceId;
            spanId = activeSpan.SpanId;
            return;
        }

        if (hub.GetScope() is { } scope)
        {
            traceId = scope.PropagationContext.TraceId;
            spanId = null;
            return;
        }

        Debug.Assert(hub is not Hub, "In case of a 'full' Hub, there is always a Scope. Otherwise (disabled) there is no Scope, but this branch should be unreachable.");
        traceId = SentryId.Empty;
        spanId = null;
    }
}
