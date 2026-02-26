using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.OpenTelemetry.Exporter.OpenTelemetryProtocol;

#if SENTRY_DSN_DEFINED_IN_ENV
var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
          ?? throw new InvalidOperationException("SENTRY_DSN environment variable is not set");
#else
// A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
// See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
var dsn = SamplesShared.Dsn;
#endif

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddOpenTelemetry().WithTracing(builder =>
        {
            builder
                .AddSentryOtlp(dsn) // <-- Configure OpenTelemetry to send traces to Sentry
                .AddHttpClientInstrumentation(); // From OpenTelemetry.Instrumentation.Http... adds automatic tracing for outgoing HTTP requests
        });
    })
    .ConfigureLogging(logging =>
    {
        logging.AddSentry(options =>
        {
            options.Dsn = dsn;
            options.TracesSampleRate = 1.0;
            options.UseOtlp(); // <-- Configure Sentry to use open telemetry
            options.DisableSentryHttpMessageHandler = true; // So Sentry doesn't also create spans for outbound HTTP requests
            options.Debug = true;
        });
    })
    .Build();

await host.RunAsync();
