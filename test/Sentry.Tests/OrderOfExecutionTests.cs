using Sentry.Testing;

namespace Sentry.Tests;

[UsesVerify]
public class OrderOfExecutionTests
{
    [Fact]
    public Task Event() =>
        RunTest((hub, events) => hub.CaptureEvent(new()));

    [Fact]
    public Task EventWithScope() =>
        RunTest((hub, events) => hub.CaptureEvent(new(), _ => AddScopedCapture(_, events)));

    [Fact]
    public Task Exception() =>
        RunTest((hub, events) => hub.CaptureException(new()));

    [Fact]
    public Task ExceptionWithScope() =>
        RunTest((hub, events) => hub.CaptureException(new(), _ => AddScopedCapture(_, events)));

    [Fact]
    public Task Message() =>
        RunTest((hub, events) => hub.CaptureMessage("The message"));

    [Fact]
    public Task MessageWithScope() =>
        RunTest((hub, events) => hub.CaptureMessage("The message", _ => AddScopedCapture(_, events)));

    static async Task RunTest(Action<IHub, List<string>> action)
    {
        var events = new List<string>();
        var options = GetOptions(events);
        AddGlobalCapture(options, events);

        var hub = new Hub(
            options,
            sessionManager: new SessionManager(events));
        var transaction = hub.StartTransaction("name", "operation");
        hub.ConfigureScope(scope =>
        {
            scope.Transaction = transaction;
            AddScopedCapture(scope, events);
        });
        hub.StartSession();
        action(hub, events);
        hub.EndSession();
        transaction.Finish();
        await Verify(events);
    }

    static SentryOptions GetOptions(List<string> events)
    {
        return new()
        {
            TracesSampleRate = 1,
            Transport = new FakeTransport(),
            Dsn = ValidDsn,
            ClientReportRecorder = new ClientReportRecorder(events)
        };
    }

    static void AddScopedCapture(Scope scope, List<string> events)
    {
        var capture = new Capture("scoped", events);
        scope.AddExceptionProcessor(capture);
        scope.AddEventProcessor(capture);
        scope.AddTransactionProcessor(capture);
    }

    static void AddGlobalCapture(SentryOptions options, List<string> events)
    {
        options.BeforeSend += _ =>
        {
            events.Add("global BeforeSend");
            return _;
        };
        options.TracesSampler += _ =>
        {
            events.Add("global TracesSampler");
            return null;
        };
        var capture = new Capture("global", events);
        options.AddExceptionFilter(capture);
        options.AddExceptionProcessor(capture);
        options.AddEventProcessor(capture);
        options.AddTransactionProcessor(capture);
    }

    class Capture :
        IExceptionFilter,
        ISentryEventProcessor,
        ISentryTransactionProcessor, ISentryEventExceptionProcessor
    {
        string context;
        List<string> events;

        public Capture(string context, List<string> events)
        {
            this.context = context;
            this.events = events;
        }

        bool IExceptionFilter.Filter(Exception ex)
        {
            AddEvent("ExceptionFilter");
            return false;
        }

        SentryEvent ISentryEventProcessor.Process(SentryEvent @event)
        {
            AddEvent("EventProcessor");
            return @event;
        }

        Transaction ISentryTransactionProcessor.Process(Transaction transaction)
        {
            AddEvent("TransactionProcessor");
            return transaction;
        }

        void ISentryEventExceptionProcessor.Process(Exception exception, SentryEvent sentryEvent) =>
            AddEvent("EventExceptionProcessor");

        void AddEvent(string name)
        {
            events.Add($"{context} {name}");
        }
    }

    class SessionManager : ISessionManager
    {
        List<string> events;
        Session session;

        public SessionManager(List<string> events) =>
            this.events = events;

        public bool IsSessionActive { get; }

        public SessionUpdate TryRecoverPersistedSession()
        {
            throw new NotImplementedException();
        }

        public SessionUpdate StartSession()
        {
            events.Add("SessionManager StartSession");
            session = new(null, "release", null);
            return session.CreateUpdate(true, DateTimeOffset.UtcNow);
        }

        public SessionUpdate EndSession(DateTimeOffset timestamp, SessionEndStatus status)
        {
            events.Add($"SessionManager EndSession {status}");
            return session.CreateUpdate(false, timestamp, status);
        }

        public SessionUpdate EndSession(SessionEndStatus status) =>
            EndSession(DateTimeOffset.UtcNow, status);

        public void PauseSession() =>
            throw new NotImplementedException();

        public IReadOnlyList<SessionUpdate> ResumeSession() =>
            throw new NotImplementedException();

        public SessionUpdate ReportError()
        {
            events.Add("SessionManager ReportError");
            session.ReportError();
            return session.CreateUpdate(false, DateTimeOffset.UtcNow);
        }
    }

    class ClientReportRecorder : IClientReportRecorder
    {
        List<string> events;

        public ClientReportRecorder(List<string> events) =>
            this.events = events;

        public void RecordDiscardedEvent(DiscardReason reason, DataCategory category) =>
            events.Add($"Discard ClientReport. {reason} {category}");

        public ClientReport GenerateClientReport()
        {
            throw new NotImplementedException();
        }

        public void Load(ClientReport report)
        {
        }
    }
}
