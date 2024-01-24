namespace Sentry;

/// <summary>
/// SDK API contract which combines a client and scope management.
/// </summary>
/// <remarks>
/// The contract of which <see cref="T:Sentry.SentrySdk" /> exposes statically.
/// This interface exist to allow better testability of integrations which otherwise
/// would require dependency to the static <see cref="T:Sentry.SentrySdk" />.
/// </remarks>
/// <inheritdoc cref="ISentryClient" />
/// <inheritdoc cref="ISentryScopeManager" />
public interface IHub :
    ISentryClient,
    ISentryScopeManager
{
    /// <summary>
    /// Last event id recorded in the current scope.
    /// </summary>
    SentryId LastEventId { get; }

    /// <summary>
    /// <inheritdoc cref="IMetricAggregator"/>
    /// </summary>
    IMetricAggregator Metrics { get; }

    /// <summary>
    /// Starts a transaction.
    /// </summary>
    ITransactionTracer StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext);

    /// <summary>
    /// Binds specified exception the specified span.
    /// </summary>
    /// <remarks>
    /// This method is used internally and is not meant for public use.
    /// </remarks>
    void BindException(Exception exception, ISpan span);

    /// <summary>
    /// Gets the currently ongoing (not finished) span or <code>null</code> if none available.
    /// </summary>
    ISpan? GetSpan();

    /// <summary>
    /// Gets the Sentry trace header that allows tracing across services
    /// </summary>
    SentryTraceHeader? GetTraceHeader();

    /// <summary>
    /// Gets the Sentry baggage header that allows tracing across services
    /// </summary>
    BaggageHeader? GetBaggage();

    /// <summary>
    /// Continues a trace based on HTTP header values provided as strings.
    /// </summary>
    /// <remarks>
    /// If no "sentry-trace" header is provided a random trace ID and span ID is created.
    /// </remarks>
    TransactionContext ContinueTrace(
        string? traceHeader,
        string? baggageHeader,
        string? name = null,
        string? operation = null);

    /// <summary>
    /// Continues a trace based on HTTP header values.
    /// </summary>
    /// <remarks>
    /// If no "sentry-trace" header is provided a random trace ID and span ID is created.
    /// </remarks>
    TransactionContext ContinueTrace(
        SentryTraceHeader? traceHeader,
        BaggageHeader? baggageHeader,
        string? name = null,
        string? operation = null);

    /// <summary>
    /// Starts a new session.
    /// </summary>
    void StartSession();

    /// <summary>
    /// Pauses an active session.
    /// </summary>
    void PauseSession();

    /// <summary>
    /// Resumes an active session.
    /// If the session has been paused for longer than the duration of time specified in
    /// <see cref="SentryOptions.AutoSessionTrackingInterval"/> then the paused session is
    /// ended and a new one is started instead.
    /// </summary>
    void ResumeSession();

    /// <summary>
    /// Ends the currently active session.
    /// </summary>
    void EndSession(SessionEndStatus status = SessionEndStatus.Exited);

    /// <summary>
    /// Captures an event with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="evt">The event to be captured.</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns></returns>
    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope);

    /// <summary>
    /// Captures an event with a configurable scope.
    /// </summary>
    /// <remarks>
    /// This allows modifying a scope without affecting other events.
    /// </remarks>
    /// <param name="evt">The event to be captured.</param>
    /// <param name="hint">An optional hint to be provided with the event</param>
    /// <param name="configureScope">The callback to configure the scope.</param>
    /// <returns></returns>
    public SentryId CaptureEvent(SentryEvent evt, Hint? hint, Action<Scope> configureScope);
}
