# Overview

This sample demonstrates how an ASP.NET Core application that is instrumented with the OpenTelemetry .NET SDK can be 
configured to send trace information to Sentry, with the following initialization code:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddOpenTelemetryWithSentry(
    tracerProviderBuilder => tracerProviderBuilder
        .AddSource(Telemetry.ActivitySource.Name)
        .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation(),
    options =>
    {
        options.Dsn = "...Your DSN...";
        options.TracesSampleRate = 1.0;
        options.Debug = builder.Environment.IsDevelopment();
    }
);
```

## Customizing propagation

Sentry's OpenTelemetry integration sets the DefaultTextMapPropagator for OpenTelemetry to a `SentryPropagator`. This 
propagator ensures that both the W3C baggage header and the sentry-trace header get propagated from upstream services 
and/or to downstream services. 

If you need to further customize header propagation in your application (e.g. propagating other vendor specific headers)
then you can do so by creating a `CompositeTextMapPropagator` consisting of the custom propagator(s) you need plus the
`SentryPropagator`. You can supply this as a third (optional) parameter to the `AddOpenTelemetryWithSentry` extension 
method illustrated above.
