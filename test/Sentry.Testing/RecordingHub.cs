public class RecordingHub : IHub
{
    private ConcurrentBag<SentryEvent> _events = new();
    public IEnumerable<SentryEvent> Events => _events;

    private ConcurrentBag<UserFeedback> _userFeedback = new();
    public IEnumerable<UserFeedback> UserFeedbacks => _userFeedback;

    private ConcurrentBag<Transaction> _transactions = new();
    public IEnumerable<Transaction> Transactions => _transactions;

    private ConcurrentBag<SessionUpdate> _sessionUpdates = new();
    public IEnumerable<SessionUpdate> SessionUpdates => _sessionUpdates;

    public bool IsEnabled => true;

    public SentryId CaptureEvent(SentryEvent evt, Scope scope = null)
    {
        _events.Add(evt);
        return SentryId.Create();
    }

    public SentryId LastEventId { get; }

    public ITransaction StartTransaction(ITransactionContext context, IReadOnlyDictionary<string, object> customSamplingContext) =>
        throw new NotImplementedException();

    public void BindException(Exception exception, ISpan span) =>
        throw new NotImplementedException();

    public ISpan GetSpan() =>
        throw new NotImplementedException();

    public SentryTraceHeader GetTraceHeader() =>
        throw new NotImplementedException();

    public void StartSession() =>
        throw new NotImplementedException();

    public void PauseSession() =>
        throw new NotImplementedException();

    public void ResumeSession() =>
        throw new NotImplementedException();

    public void EndSession(SessionEndStatus status = SessionEndStatus.Exited) =>
        throw new NotImplementedException();

    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
    {
        _events.Add(evt);
        return SentryId.Create();
    }

    public void CaptureUserFeedback(UserFeedback userFeedback) =>
        _userFeedback.Add(userFeedback);

    public void CaptureTransaction(Transaction transaction) =>
        _transactions.Add(transaction);

    public void CaptureSession(SessionUpdate sessionUpdate) =>
        _sessionUpdates.Add(sessionUpdate);

    public Task FlushAsync(TimeSpan timeout) =>
        throw new NotImplementedException();

    public void ConfigureScope(Action<Scope> configureScope) =>
        throw new NotImplementedException();

    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) =>
        throw new NotImplementedException();

    public void BindClient(ISentryClient client) =>
        throw new NotImplementedException();

    public IDisposable PushScope() =>
        throw new NotImplementedException();

    public IDisposable PushScope<TState>(TState state) =>
        throw new NotImplementedException();

    public void WithScope(Action<Scope> scopeCallback) =>
        throw new NotImplementedException();
}
