using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Extensions;

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
            s => s.AddBreadcrumb(breadcrumb, hint ?? new SentryHint())
            );
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
    public static void LockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = true);

    /// <summary>
    /// Unlocks the current scope to allow subsequent calls to <see cref="ISentryScopeManager.PushScope"/> create new scopes.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void UnlockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = false);

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

    internal static SentryId CaptureExceptionInternal(this IHub hub, Exception ex) =>
        hub.CaptureEvent(new SentryEvent(ex));

    /// <summary>
    /// Captures the exception with a configurable scope callback.
    /// </summary>
    /// <param name="hub">The Sentry hub.</param>
    /// <param name="ex">The exception.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns>The Id of the event</returns>
    public static SentryId CaptureException(this IHub hub, Exception ex, Action<Scope> configureScope) =>
        hub.CaptureEvent(new SentryEvent(ex), configureScope);

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
        ITransactionTracer? transaction = null;
        hub.ConfigureScope(scope => transaction = scope.Transaction);
        return transaction;
    }

    internal static ITransactionTracer? GetTransactionIfSampled(this IHub hub)
    {
        var transaction = hub.GetTransaction();
        return transaction?.IsSampled == true ? transaction : null;
    }

    internal static Hub? GetRealHub(this IHub hub) => hub switch
    {
        Hub thisHub => thisHub,
        HubAdapter when SentrySdk.CurrentHub is Hub sdkHub => sdkHub,
        _ => null
    };

    internal static ISpan StartSpan(this IHub hub, string operation, string description)
    {
        ITransactionTracer? currentTransaction = null;
        hub.ConfigureScope(s => currentTransaction = s.Transaction);
        return currentTransaction is { } transaction
            ? transaction.StartChild(operation, description)
            : hub.StartTransaction(description, operation, description);
    }
}
