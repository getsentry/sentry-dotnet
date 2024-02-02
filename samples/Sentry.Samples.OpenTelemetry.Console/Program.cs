/*
 * This sample demonstrates how to initialize and enable Open Telemetry with Sentry
 * in a console application.
 * For using Open Telemetry and Sentry in ASP.NET, see Sentry.Samples.OpenTelemetry.AspNet.
 * For using Open Telemetry and Sentry in ASP.NET Core, see Sentry.Samples.OpenTelemetry.AspNetCore.
 */

using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;

var serviceName = "Sentry.Samples.OpenTelemetry.Console";
var serviceVersion = "1.0.0";

SentrySdk.Init(options =>
{
    // options.Dsn = "... Your DSN ...";
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(serviceName)
    .ConfigureResource(resource =>
        resource.AddService(
            serviceName: serviceName,
            serviceVersion: serviceVersion))
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    .Build();
