# Overview

This sample demonstrates how an ASP.NET application that is instrumented with the OpenTelemetry .NET SDK can be 
configured to send trace information to Sentry, with the following initialization code in `Application_Start`:

```csharp
var builder = Sdk.CreateTracerProviderBuilder()
    .AddAspNetInstrumentation()
    .AddSource(Telemetry.ServiceName)
    .SetResourceBuilder(
        ResourceBuilder.CreateDefault()
            .AddService(serviceName: Telemetry.ServiceName, serviceVersion: "1.0.0")
        );

// Initialize Sentry to capture AppDomain unhandled exceptions and more.
_sentry = SentrySdk.Init(o =>
{
    //o.Dsn = "...Your DSN...";
    o.Debug = true;
    o.TracesSampleRate = 1.0;
    o.AddAspNet(RequestSize.Always);
    o.UseOpenTelemetry(builder);
});

_tracerProvider = builder.Build();
```

## Customizing propagation

Sentry's OpenTelemetry integration sets the DefaultTextMapPropagator for OpenTelemetry to a `SentryPropagator`. This 
propagator ensures that both the W3C baggage header and the sentry-trace header get propagated from upstream services 
and/or to downstream services. 

If you need to further customize header propagation in your application (e.g. propagating other vendor specific headers)
then you can do so by creating a `CompositeTextMapPropagator` consisting of the custom propagator(s) you need plus the
`SentryPropagator`. You can supply this as a second (optional) parameter to the `UseOpenTelemetry` extension method 
on `SentryOptions` above.
