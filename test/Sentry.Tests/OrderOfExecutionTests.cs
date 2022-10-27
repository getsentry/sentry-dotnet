namespace Sentry.Tests;

[UsesVerify]
public class OrderOfExecutionTests
{
    [Theory]
    [MemberData(nameof(GetData))]
    public Task Event(bool alwaysDrop, bool sampleEpsilon, bool sampleOut) =>
        RunTest(
            (hub, events) => hub.CaptureEvent(new()),
            alwaysDrop, sampleEpsilon, sampleOut);

    [Theory]
    [MemberData(nameof(GetData))]
    public Task Exception(bool alwaysDrop, bool sampleEpsilon, bool sampleOut) =>
        RunTest(
            (hub, events) => hub.CaptureException(new()),
            alwaysDrop, sampleEpsilon, sampleOut);

    [Theory]
    [MemberData(nameof(GetData))]
    public Task Message(bool alwaysDrop, bool sampleEpsilon, bool sampleOut) =>
        RunTest(
            (hub, events) => hub.CaptureMessage("The message"),
            alwaysDrop, sampleEpsilon, sampleOut);

    [Theory]
    [MemberData(nameof(GetData))]
    public Task UserFeedback(bool alwaysDrop, bool sampleEpsilon, bool sampleOut) =>
        RunTest(
            (hub, events) => hub.CaptureUserFeedback(new(SentryId.Create(), "Use Feedback", null, null)),
            alwaysDrop, sampleEpsilon, sampleOut);

    public static IEnumerable<object[]> GetData()
    {
        foreach (var alwaysDrop in new[] {true, false})
        foreach (var sampleEpsilon in new[] {true, false})
        foreach (var sampleOut in new[] {true, false})
        {
            yield return new object[] {alwaysDrop, sampleEpsilon, sampleOut};
        }
    }

    static async Task RunTest(Action<IHub, List<string>> action, bool alwaysDrop, bool sampleEpsilon, bool sampleOut)
    {
        float? SampleRate()
        {
            if (sampleEpsilon)
            {
                return float.Epsilon;
            }

            if (sampleOut)
            {
                return null;
            }

            return 1;
        }

        var events = new List<string>();
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            SampleRate = SampleRate(),
            Transport = transport,
            Dsn = ValidDsn,
            ClientReportRecorder = new ClientReportRecorder(events)
        };
        if (alwaysDrop)
        {
            options.SampleRate = float.Epsilon;
        }

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
        var globalCapture = new Capture("global", events);
        options.AddExceptionFilter(globalCapture);
        options.AddExceptionProcessor(globalCapture);
        options.AddEventProcessor(globalCapture);
        options.AddTransactionProcessor(globalCapture);

        var hub = new Hub(
            options,
            sessionManager: new SessionManager(events));
        var transaction = hub.StartTransaction("name", "operation");
        hub.ConfigureScope(scope =>
        {
            scope.Transaction = transaction;
            var scopedCapture = new Capture("scoped", events);
            scope.AddExceptionProcessor(scopedCapture);
            scope.AddEventProcessor(scopedCapture);
            scope.AddTransactionProcessor(scopedCapture);
        });
        hub.StartSession();
        action(hub, events);
        hub.EndSession();
        transaction.Finish();
        await Verify(
                new
                {
                    events,
                    transport.Envelopes
                })
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("Stacktrace", "public_key", "Description", "User", "Platform", "Request", "release", "Release", "sdk", "environment", "Environment")
            .IgnoreMember<SentryEvent>(_ => _.SentryThreads)
            .UseParameters(alwaysDrop, sampleEpsilon, sampleOut);
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
