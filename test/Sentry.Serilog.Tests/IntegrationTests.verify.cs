#if NET8_0_OR_GREATER
namespace Sentry.Serilog.Tests;

[Collection(nameof(SentrySdkCollection))]
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
                _.AttachStacktrace = false;
                _.Release = "test-release";
            });

        Log.Logger = configuration.CreateLogger();
        using (LogContext.PushProperty("MyTaskId", 42))
        using (LogContext.PushProperty(
                   "inventory",
                   new
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
            .UniqueForRuntimeAndVersion()
            .IgnoreStandardSentryMembers();
    }

    [Fact]
    public Task LoggingInsideTheContextOfLogging()
    {
        var transport = new RecordingTransport();

        var configuration = new LoggerConfiguration();

        var diagnosticLogger = new InMemoryDiagnosticLogger();
        configuration.WriteTo.Sentry(
            _ =>
            {
                _.TracesSampleRate = 1;
                _.Transport = transport;
                _.DiagnosticLogger = diagnosticLogger;
                _.Dsn = ValidDsn;
                _.Debug = true;
                _.AttachStacktrace = false;
                _.Release = "test-release";
            });

        Log.Logger = configuration.CreateLogger();

        SentrySdk.ConfigureScope(
            scope =>
            {
                scope.OnEvaluating += (_, _) => Log.Error("message from OnEvaluating");
                Log.Error("message");
            });
        Log.CloseAndFlush();

        return Verify(
                new
                {
                    diagnosticLoggerEntries = diagnosticLogger
                        .Entries
                        .Where(_ => _.Level == SentryLevel.Error),
                    transport.Envelopes
                })
            .IgnoreStandardSentryMembers();
    }
}
#endif
