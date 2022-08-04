[UsesVerify]
public class IntegrationTests
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
            Dsn = ValidDsn
        };

        var hub = SentrySdk.InitHub(options);
        using var sdk = SentrySdk.UseHub(hub);

        var hierarchy = SetupLogging(hub);

        var log = LogManager.GetLogger(typeof(IntegrationTests));
        log.Error("The message");

        hierarchy.Flush(10000);

        return Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("ThreadName", "Domain", "LineNumber");
    }

    [Fact]
    public Task LoggingInsideTheContextOfLogging()
    {
        var transport = new RecordingTransport();
        var diagnosticLogger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions
        {
            TracesSampleRate = 1,
            Debug = true,
            DiagnosticLogger = diagnosticLogger,
            DiagnosticLevel = SentryLevel.Debug,
            Transport = transport,
            Dsn = ValidDsn
        };

        var hub = SentrySdk.InitHub(options);
        using var sdk = SentrySdk.UseHub(hub);

        var hierarchy = SetupLogging(hub);

        var log = LogManager.GetLogger(typeof(IntegrationTests));
        SentrySdk.ConfigureScope(
            scope =>
            {
                scope.OnEvaluating += (_, _) =>
                    log.Error("message from OnEvaluating");
                log.Error("message");
            });

        log.Error("The message");

        hierarchy.Flush(10000);

        return Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("Extra");
    }

    private static Hierarchy SetupLogging(IHub hub)
    {
        var hierarchy = (Hierarchy) LogManager.GetRepository(typeof(IntegrationTests).Assembly);
        var layout = new PatternLayout
        {
            ConversionPattern = "%message%"
        };

        layout.ActivateOptions();

        var tracer = new TraceAppender
        {
            Layout = layout
        };

        tracer.ActivateOptions();
        hierarchy.Root.AddAppender(tracer);

        var appender = new SentryAppender(
            _ => Substitute.For<IDisposable>(),
            hub)
        {
            Layout = layout,
            Dsn = ValidDsn,
            SendIdentity = true
        };
        appender.ActivateOptions();
        hierarchy.Root.AddAppender(appender);

        hierarchy.Root.Level = Level.All;
        hierarchy.Configured = true;
        return hierarchy;
    }
}
