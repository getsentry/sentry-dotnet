#if NET6_0_OR_GREATER
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

    [Fact]
    public Task StructuredLogging()
    {
        var transport = new RecordingTransport();

        var configuration = new LoggerConfiguration();
        configuration.MinimumLevel.Debug();
        var diagnosticLogger = new InMemoryDiagnosticLogger();
        configuration.WriteTo.Sentry(
            _ =>
            {
                _.MinimumEventLevel = (LogEventLevel)int.MaxValue;
                _.Experimental.EnableLogs = true;
                _.Transport = transport;
                _.DiagnosticLogger = diagnosticLogger;
                _.Dsn = ValidDsn;
                _.Debug = true;
                _.Environment = "test-environment";
                _.Release = "test-release";
            });

        Log.Logger = configuration.CreateLogger();

        Log.Debug("Debug message with a Scalar property: {Scalar}", 42);
        Log.Information("Information message with a Sequence property: {Sequence}", new object[] { new int[] { 41, 42, 43 } });
        Log.Warning("Warning message with a Dictionary property: {Dictionary}", new Dictionary<string, string> { { "key", "value" } });
        Log.Error("Error message with a Structure property: {Structure}", (Number: 42, Text: "42"));

        Log.CloseAndFlush();

        var envelopes = transport.Envelopes;
        var logs = transport.Payloads.OfType<JsonSerializable>()
            .Select(payload => payload.Source)
            .OfType<StructuredLog>()
            .Select(log => log.Items.ToArray());
        var diagnostics = diagnosticLogger.Entries.Where(_ => _.Level >= SentryLevel.Warning);
        return Verify(new { envelopes, logs, diagnostics })
            .IgnoreStandardSentryMembers();
    }
}
#endif
