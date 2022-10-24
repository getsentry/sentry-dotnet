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
    public Task Message() =>
        RunTest((hub, events) => hub.CaptureMessage("The message"));

    static async Task RunTest(Action<IHub, List<string>> action)
    {
        var events = new List<string>();
        var options = GetOptions();
        AddGlobalCapture(options, events);

        var hub = SentrySdk.InitHub(options);
        var transaction = hub.StartTransaction("name", "operation");
        hub.ConfigureScope(scope =>
        {
            scope.Transaction = transaction;
            AddScopedCapture(scope, events);
        });
        action(hub, events);
        transaction.Finish();
        await Verify(events);
    }

    static SentryOptions GetOptions()
    {
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Transport = new FakeTransport(),
            Dsn = ValidDsn,
        };
        return options;
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
}
