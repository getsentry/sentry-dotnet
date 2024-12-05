namespace Sentry.Tests;

public class EventProcessorTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public EventProcessorTests(ITestOutputHelper output)
    {
        _logger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public async Task Simple()
    {
        var transport = new RecordingTransport();
        var options = Options(transport);
        options.AddEventProcessor(new TheEventProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            hub.CaptureMessage("TheMessage");
            await hub.FlushAsync();
        }

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    [Fact]
    public async Task WithTransaction()
    {
        var transport = new RecordingTransport();
        var options = Options(transport);

        options.AddEventProcessor(new TheEventProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureMessage("TheMessage");
            await hub.FlushAsync();
        }

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class TheEventProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event)
        {
            @event.Contexts["key"] = "value";
            return @event;
        }
    }

    [Fact]
    public async Task Discard()
    {
        var transport = new RecordingTransport();
        var options = Options(transport);
        options.AddEventProcessor(new DiscardEventProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            hub.CaptureMessage("TheMessage");
            await hub.FlushAsync();
        }

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class DiscardEventProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event) => null;
    }

    private SentryOptions Options(ITransport transport) =>
        new()
        {
            TracesSampleRate = 1,
            Debug = true,
            Transport = transport,
            Dsn = ValidDsn,
            DiagnosticLogger = _logger,
            AttachStacktrace = false,
            Release = "release",
            InitNativeSdks = false
        };
}
