namespace Sentry.Tests;

public partial class TransactionProcessorTests
{
    private readonly TestOutputDiagnosticLogger _logger;

    public TransactionProcessorTests(ITestOutputHelper output)
    {
        _logger = new TestOutputDiagnosticLogger(output);
    }

    [Fact]
    public void SampledOut()
    {
        var transport = Substitute.For<ITransport>();
        var options = Options(transport);
        var processor = new TrackingProcessor();
        options.AddTransactionProcessor(processor);
        var transaction = new SentryTransaction("name", "operation")
        {
            IsSampled = false
        };

        using var hub = new Hub(options);
        hub.CaptureTransaction(transaction);

        Assert.False(processor.Called);
    }

    public class TheProcessor : ISentryTransactionProcessor
    {
        public SentryTransaction Process(SentryTransaction transaction)
        {
            transaction.Contexts["key"] = "value";
            return transaction;
        }
    }

    public class TrackingProcessor : ISentryTransactionProcessor
    {
        public bool Called { get; private set; }

        public SentryTransaction Process(SentryTransaction transaction)
        {
            Called = true;
            return transaction;
        }
    }

    public class DiscardProcessor : ISentryTransactionProcessor
    {
        public SentryTransaction Process(SentryTransaction transaction) =>
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
            Release = "release",
            InitNativeSdks = false
        };
}
