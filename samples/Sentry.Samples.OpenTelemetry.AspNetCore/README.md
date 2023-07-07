# Overview

This sample demonstrates how an ASP.NET Core application that is instrumented with the OpenTelemetry .NET SDK can be 
configured to send trace information to Sentry.

There are a couple of ways you can do this. One way is to use extensions from the `Sentry.OpenTelemetry` package. The 
other (used in this sample) uses an extension from the `Sentry.OpenTelemetry.AspNetCore` package.

## Method 1: Using the Sentry.OpenTelemetry extensions

Call `AddSentry` on the `TracerProviderBuilder` that you're using to configure OpenTelemetry in your application, to 
ensure OpenTelemetry Activity's get sent to Sentry as events and that Sentry headers get correctly propagated in 
distributed applications. 

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder => 
        tracerProviderBuilder
            .AddSource(Telemetry.ActivitySource.Name)
            .ConfigureResource(resource => resource.AddService(Telemetry.ServiceName))
            .AddSentry();
    );
```

Call `UseOpenTelemetry` on the `SentryOptions` that you are using to configure Sentry in your project, to ensure that
Transactions get created in Sentry for OpenTelemetry root spans. Transactions are used in Sentry to send groups of 
related spans to the Sentry server, so without these, no tracing information will be sent to your Sentry server.

```csharp
builder.WebHost.UseSentry(options => {
    options.Dsn = new Dsn("... Your DSN ...");
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry();
});
```

## Method 2: Using the Sentry.OpenTelemetry.AspNetCore extension

Alternatively, the above can be achieved more simply by using a single extension method from the 
`Sentry.OpenTelemetry.AspNetCore` package, as demonstrated in this Sample:

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
`SentryPropagator`. You can supply this as a third (optional) parameter to either the `AddSentry` extension method (if 
you're using _Method 1_ above) or the `AddOpenTelemetryWithSentry` extension method (if you're using _Method 2_ above).
