[UsesVerify]
public class IntegrationTests
{
    [Fact]
    public Task Simple()
    {
        var transport = new RecordingTransport();
        var diagnosticLogger = new InMemoryDiagnosticLogger();
        var options = new SentryOptions {
            TracesSampleRate = 1,
            Debug = true,
            DiagnosticLogger = diagnosticLogger,
            Transport = transport,
            Dsn = ValidDsn
        };

        var hub = SentrySdk.InitHub(options);
        using var sdk = SentrySdk.UseHub(hub);

        SetupLogging(hub);

        var log = LogManager.GetLogger(typeof(IntegrationTests));
        log.Debug("The message");
        LogManager.Flush(1000);
        return Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("ThreadName", "Domain");
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

        SetupLogging(hub);

        SentrySdk.ConfigureScope(
            scope =>
            {
                var log = LogManager.GetLogger(typeof(IntegrationTests));
                scope.OnEvaluating += (_, _) =>
                    log.Error("message from OnEvaluating");
                log.Error("message");
            });

        var log = LogManager.GetLogger(typeof(IntegrationTests));
        log.Error("The message");

        LogManager.Flush(1000);

        return Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("ThreadName", "Domain");
    }

    private static void SetupLogging(IHub hub)
    {
        var hierarchy = (Hierarchy) LogManager.GetRepository();
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
    }
}
