[UsesVerify]
public class EventProcessorTests
{
    [Fact]
    public Task Simple()
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
        using var sdk = SentrySdk.UseHub(hub);
        hub.CaptureMessage("TheMessage");

        return Verify(transport.Envelopes)
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
}
