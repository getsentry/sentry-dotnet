using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;
using Sentry.Protocol.Metrics;

namespace Sentry.Extensibility;

/// <summary>
/// An implementation of <see cref="IHub" /> which forwards any call to <see cref="SentrySdk" />.
/// </summary>
/// <remarks>
/// Allows testing classes which otherwise would need to depend on static <see cref="SentrySdk" />
/// by having them depend on <see cref="IHub"/> instead, which can be mocked.
/// </remarks>
/// <inheritdoc cref="IHub" />
[DebuggerStepThrough]
public sealed class HubAdapter : IHub
{
    /// <summary>
    /// The single instance which forwards all calls to <see cref="SentrySdk"/>
    /// </summary>
    public static readonly HubAdapter Instance = new();

    private HubAdapter() { }

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    public bool IsEnabled { [DebuggerStepThrough] get => SentrySdk.IsEnabled; }

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    public SentryId LastEventId { [DebuggerStepThrough] get => SentrySdk.LastEventId; }

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void ConfigureScope(Action<Scope> configureScope)
        => SentrySdk.ConfigureScope(configureScope);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
        => SentrySdk.ConfigureScopeAsync(configureScope);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public IDisposable PushScope()
        => SentrySdk.PushScope();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public IDisposable PushScope<TState>(TState state)
        => SentrySdk.PushScope(state);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext)
        => SentrySdk.StartTransaction(context, customSamplingContext);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    internal ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext,
        DynamicSamplingContext? dynamicSamplingContext)
        => SentrySdk.StartTransaction(context, customSamplingContext, dynamicSamplingContext);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void BindException(Exception exception, ISpan span) =>
        SentrySdk.BindException(exception, span);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public ISpan? GetSpan()
        => SentrySdk.GetSpan();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public SentryTraceHeader? GetTraceHeader()
        => SentrySdk.GetTraceHeader();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public BaggageHeader? GetBaggage()
        => SentrySdk.GetBaggage();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public TransactionContext ContinueTrace(
        string? traceHeader,
        string? baggageHeader,
        string? name = null,
        string? operation = null)
        => SentrySdk.ContinueTrace(traceHeader, baggageHeader, name, operation);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public TransactionContext ContinueTrace(
        SentryTraceHeader? traceHeader,
        BaggageHeader? baggageHeader,
        string? name = null,
        string? operation = null)
        => SentrySdk.ContinueTrace(traceHeader, baggageHeader, name, operation);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void StartSession()
        => SentrySdk.StartSession();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void PauseSession()
        => SentrySdk.PauseSession();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void ResumeSession()
        => SentrySdk.ResumeSession();

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
        => SentrySdk.EndSession(status);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void BindClient(ISentryClient client)
        => SentrySdk.BindClient(client);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public void AddBreadcrumb(
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
        => SentrySdk.AddBreadcrumb(message, category, type, data, level);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddBreadcrumb(
        ISystemClock clock,
        string message,
        string? category = null,
        string? type = null,
        IDictionary<string, string>? data = null,
        BreadcrumbLevel level = default)
        => SentrySdk.AddBreadcrumb(
            clock,
            message,
            category,
            type,
            data,
            level);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public SentryId CaptureEvent(SentryEvent evt)
        => SentrySdk.CaptureEvent(evt);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SentryId CaptureEvent(SentryEvent evt, Scope? scope)
        => SentrySdk.CaptureEvent(evt, scope, null);

    /// <inheritdoc cref="ISentryClient.CaptureEnvelope"/>
    public bool CaptureEnvelope(Envelope envelope) => SentrySdk.CurrentHub.CaptureEnvelope(envelope);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SentryId CaptureEvent(SentryEvent evt, Scope? scope, SentryHint? hint = null)
        => SentrySdk.CaptureEvent(evt, scope, hint);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
        => SentrySdk.CaptureEvent(evt, configureScope);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    public SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Action<Scope> configureScope)
        => SentrySdk.CaptureEvent(evt, hint, configureScope);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    public SentryId CaptureException(Exception exception)
        => SentrySdk.CaptureException(exception);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CaptureTransaction(SentryTransaction transaction)
        => SentrySdk.CaptureTransaction(transaction);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint)
        => SentrySdk.CaptureTransaction(transaction, scope, hint);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CaptureSession(SessionUpdate sessionUpdate)
        => SentrySdk.CaptureSession(sessionUpdate);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    public SentryId CaptureCheckIn(SentryCheckIn checkIn)
        => SentrySdk.CaptureCheckIn(checkIn);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>.
    /// </summary>
    public SentryId CaptureCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null)
        => SentrySdk.CaptureCheckIn(monitorSlug, status, sentryId);

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Task FlushAsync(TimeSpan timeout)
        => SentrySdk.FlushAsync(timeout);

    /// <inheritdoc cref="IMetricAggregator"/>
    public IMetricAggregator Metrics
        => SentrySdk.Metrics;

    /// <summary>
    /// Forwards the call to <see cref="SentrySdk"/>
    /// </summary>
    [DebuggerStepThrough]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void CaptureUserFeedback(UserFeedback sentryUserFeedback)
        => SentrySdk.CaptureUserFeedback(sentryUserFeedback);
}
