namespace Sentry.Extensibility;

/// <summary>
/// Disabled Hub.
/// </summary>
public class DisabledHub : IHub, IDisposable
{
    /// <summary>
    /// The singleton instance.
    /// </summary>
    public static readonly DisabledHub Instance = new();

    /// <summary>
    /// Always disabled.
    /// </summary>
    public bool IsEnabled => false;

    private DisabledHub()
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void ConfigureScope(Action<Scope> configureScope)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

    /// <summary>
    /// No-Op.
    /// </summary>
    public IDisposable PushScope() => this;

    /// <summary>
    /// No-Op.
    /// </summary>
    public IDisposable PushScope<TState>(TState state) => this;

    /// <summary>
    /// No-Op.
    /// </summary>
    public void WithScope(Action<Scope> scopeCallback)
    {
    }

    /// <summary>
    /// Returns a dummy transaction.
    /// </summary>
    public ITransaction StartTransaction(
        ITransactionContext context,
        IReadOnlyDictionary<string, object?> customSamplingContext) =>
        // Transactions from DisabledHub are always sampled out
        new TransactionTracer(this, context) { IsSampled = false };

    /// <summary>
    /// No-Op.
    /// </summary>
    public void BindException(Exception exception, ISpan span)
    {
    }

    /// <summary>
    /// Returns null.
    /// </summary>
    public ISpan? GetSpan() => null;

    /// <summary>
    /// Returns null.
    /// </summary>
    public SentryTraceHeader? GetTraceHeader() => null;

    /// <summary>
    /// Returns null.
    /// </summary>
    public BaggageHeader? GetBaggage() => null;

    /// <summary>
    /// Returns sampled out transaction context.
    /// </summary>
    public TransactionContext ContinueTrace(
        string? traceHeader,
        string? baggageHeader,
        string? name = null,
        string? operation = null)
    {
        // Transactions from DisabledHub are always sampled out
        return new TransactionContext( name ?? string.Empty, operation ?? string.Empty, isSampled: false);
    }

    /// <summary>
    /// Returns sampled out transaction context.
    /// </summary>
    public TransactionContext ContinueTrace(
        SentryTraceHeader? traceHeader,
        BaggageHeader? baggageHeader,
        string? name = null,
        string? operation = null)
    {
        // Transactions from DisabledHub are always sampled out
        return new TransactionContext( name ?? string.Empty, operation ?? string.Empty, isSampled: false);
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void StartSession()
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void PauseSession()
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void ResumeSession()
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void BindClient(ISentryClient client)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null) => SentryId.Empty;

    /// <summary>
    /// No-Op.
    /// </summary>
    public SentryId CaptureEvent(SentryEvent evt, Hint? hint, Scope? scope = null) => SentryId.Empty;

    /// <summary>
    /// No-Op.
    /// </summary>
    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope) => SentryId.Empty;

    /// <summary>
    /// No-Op.
    /// </summary>
    public void CaptureTransaction(Transaction transaction)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void CaptureTransaction(Transaction transaction, Hint? hint)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void CaptureSession(SessionUpdate sessionUpdate)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

    /// <summary>
    /// No-Op.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public void CaptureUserFeedback(UserFeedback userFeedback)
    {
    }

    /// <summary>
    /// No-Op.
    /// </summary>
    public SentryId LastEventId => SentryId.Empty;
}
