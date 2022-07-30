[UsesVerify]
public class IntegrationTests
{
    [Fact]
    public Task Simple()
    {
        var hub = new RecordingHub();

        var hierarchy = (Hierarchy)LogManager.GetRepository();
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


        var log = LogManager.GetLogger(typeof(IntegrationTests));
        log.Debug("The message");

        return Verify(hub.Events)
            .IgnoreStandardSentryMembers()
            .IgnoreMembers("ThreadName", "Domain");
    }
}
