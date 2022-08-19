namespace Sentry.Tests;

[UsesVerify]
public class EventProcessorTests
{
    [Fact]
    public async Task Simple()
    {
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Debug = true,
            Transport = transport,
            Dsn = ValidDsn,
        };

        options.AddEventProcessor(new TheEventProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            hub.CaptureMessage("TheMessage");
            await hub.FlushAsync(TimeSpan.FromSeconds(1));
        }

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }
    [Fact]
    public async Task WithTransaction()
    {
        var transport = new RecordingTransport();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Debug = true,
            Transport = transport,
            Dsn = ValidDsn,
        };

        options.AddEventProcessor(new TheEventProcessor());
        var hub = SentrySdk.InitHub(options);
        using (SentrySdk.UseHub(hub))
        {
            var transaction = hub.StartTransaction("my transaction", "my operation");
            hub.ConfigureScope(scope => scope.Transaction = transaction);
            hub.CaptureMessage("TheMessage");
            await hub.FlushAsync(TimeSpan.FromSeconds(1));
        }

        await Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }

    public class TheEventProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event)
        {
            @event.Contexts["eventKey"] = "eventValue";
            return @event;
        }

        public void Process(Transaction transaction)
        {
            transaction.Contexts["transactionKey"] = "transactionValue";
        }
    }
}
