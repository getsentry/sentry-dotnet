using Sentry.Testing;

namespace Sentry.Tests;

[UsesVerify]
[Trait("Category", "Verify")]
public class TransactionProcessorTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public TransactionProcessorTests(ITestOutputHelper output)
    {
        _logger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public async Task Simple()
    {
        var transport = new RecordingTransport();
        var options = Options(transport);

        options.AddTransactionProcessor(new TheProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureMessage("TheMessage");
            transaction.Finish();
            await hub.FlushAsync(TimeSpan.FromSeconds(1));
        }

        await Verifier.Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class TheProcessor : ISentryTransactionProcessor
    {
        public Transaction Process(Transaction transaction)
        {
            transaction.Contexts["key"] = "value";
            return transaction;
        }
    }

    [Fact]
    public void SampledOut()
    {
        var transport = Substitute.For<ITransport>();
        var options = Options(transport);
        var processor = new TrackingProcessor();
        options.AddTransactionProcessor(processor);
        var transaction = new Transaction("name", "operation")
        {
            IsSampled = false
        };
        new Hub(options).CaptureTransaction(transaction);
        Assert.False(processor.Called);
    }

    public class TrackingProcessor : ISentryTransactionProcessor
    {
        public bool Called { get; private set; }

        public Transaction Process(Transaction transaction)
        {
            Called = true;
            return transaction;
        }
    }

    [Fact]
    public async Task Discard()
    {
        var transport = new RecordingTransport();
        var options = Options(transport);

        options.AddTransactionProcessor(new DiscardProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureMessage("TheMessage");
            transaction.Finish();
            await hub.FlushAsync(TimeSpan.FromSeconds(1));
        }

        await Verifier.Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class DiscardProcessor : ISentryTransactionProcessor
    {
        public Transaction Process(Transaction transaction) =>
            null;
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
            Release = "Sentry.Tests@1.0.0"
        };
}
