using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    .AddHttpClientInstrumentation() // From OpenTelemetry.Instrumentation.Http... adds automatic tracing for outgoing HTTP requests
    .Build();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureLogging(logging =>
    {
        logging.AddSentry(options =>
        {
#if !SENTRY_DSN_DEFINED_IN_ENV
            // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            options.Dsn = SamplesShared.Dsn;
#endif
            options.TracesSampleRate = 1.0;
            options.UseOpenTelemetry(); // <-- Configure Sentry to use open telemetry
            options.DisableSentryHttpMessageHandler = true; // So Sentry doesn't also create spans for outbound HTTP requests
            options.Debug = true;
        });
    })
    .Build();

await host.RunAsync();
