namespace Sentry.Tests;

[UsesVerify]
public class TransactionProcessorTests
{
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

        await Verify(transport.Envelopes)
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
        var options = new SentryOptions();
        options.AddTransactionProcessor(new TrackingProcessor());
        var transaction = new Transaction("name", "operation")
        {
            IsSampled = false
        };
        new Hub(options).CaptureTransaction(transaction);
        Assert.False(TrackingProcessor.Called);
    }

    public class TrackingProcessor : ISentryTransactionProcessor
    {
        public Transaction Process(Transaction transaction)
        {
            Called = true;
            return transaction;
        }

        public static bool Called;
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

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class DiscardProcessor : ISentryTransactionProcessor
    {
        public Transaction Process(Transaction transaction) =>
            null;
    }

    private static SentryOptions Options(RecordingTransport transport) =>
        new()
        {
            TracesSampleRate = 1,
            Debug = true,
            Transport = transport,
            Dsn = ValidDsn,
        };
}
