[UsesVerify]
public class IntegrationTests
{
    [Fact]
    public Task Simple()
    {
        var transport = new RecordingTransport();

        var configuration = new LoggerConfiguration();
        configuration.Enrich.FromLogContext();
        configuration.MinimumLevel.Debug();
        configuration.WriteTo.Sentry(
            _ =>
            {
                _.TracesSampleRate = 1;
                _.MinimumBreadcrumbLevel = LogEventLevel.Debug;
                _.MinimumEventLevel = LogEventLevel.Debug;
                _.Transport = transport;
                _.Dsn = ValidDsn;
                _.SendDefaultPii = true;
                _.TextFormatter = new MessageTemplateTextFormatter("[{MyTaskId}] {Message}");
            });

        Log.Logger = configuration.CreateLogger();
        using (LogContext.PushProperty("MyTaskId", 42))
        using (LogContext.PushProperty("inventory", new
               {
                   SmallPotion = 3,
                   BigPotion = 0,
                   CheeseWheels = 512
               }))
        {
            Log.Verbose("Verbose message which is not sent.");
            Log.Debug("Debug message stored as breadcrumb.");
            Log.ForContext("MyTaskId", 65).Debug("Message with a different MyTaskId");
            Log.Error("Some event that includes the previous breadcrumbs");

            try
            {
                throw new("Exception message");
            }
            catch (Exception exception)
            {
                exception.Data.Add("details", "Do work always throws.");
                Log.Fatal(exception, "Error: with exception");
            }
        }

        Log.CloseAndFlush();

        return Verify(transport.Envelopes)
            .IgnoreStandardSentryMembers();
    }
}
