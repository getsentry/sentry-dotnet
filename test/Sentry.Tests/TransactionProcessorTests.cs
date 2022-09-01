﻿namespace Sentry.Tests;

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
        var transport = Substitute.For<ITransport>();
        var options = new SentryOptions
        {
            Transport = transport,
            Dsn = ValidDsn
        };
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
