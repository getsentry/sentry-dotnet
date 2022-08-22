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
        public void Process(Transaction transaction) =>
            transaction.Contexts["key"] = "value";
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
        public void Process(Transaction transaction) =>
            transaction.IsSampled = false;
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
