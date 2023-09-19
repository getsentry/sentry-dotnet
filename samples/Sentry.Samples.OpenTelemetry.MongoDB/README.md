# Overview

This sample demonstrates how a MongoDB commands can be automatically instrumented using Sentry's OpenTelemetry package. 

There are a few steps to get this working, but once it's configured it shouldn't require further attention:
- Install relevant NuGet packages
- Configure Sentry OpenTelemetry Tracing
- Configure the OpenTelemetry Trace Provider to collect MongoDB diagnostic information
- Configure the OpenTelemetry Trace Provider to send trace information to Sentry
- Configure MongoDB Client to send diagnostic activity

## Install NuGet packages

For this sample, we've used the following NuGet packages
- `MongoDB.Driver`
- `MongoDB.Driver.Core.Extensions.OpenTelemetry`
- `OpenTelemetry`
- `Sentry`
- `Sentry.OpenTelemetry`

## Configure Sentry OpenTelemetry Tracing

In the SentryOptions builder:

```csharp
    options.TracesSampleRate = 1.0; // <-- Set the sample rate to 100% (in production you'd configure this to be lower)
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
```

## Configure the OpenTelemetry Trace Provider to collect MongoDB diagnostic information

On the OpenTelemetry TraceProviderBuilder:

```csharp
Sdk.CreateTracerProviderBuilder()
    .AddMongoDBInstrumentation() // <-- Adds the MongoDB OTel datasource
```

## Configure the OpenTelemetry Trace Provider to send trace information to Sentry

On the OpenTelemetry TraceProviderBuilder:

```csharp
Sdk.CreateTracerProviderBuilder()
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
```

## Configure MongoDB to send diagnostic activity

Technically, this is all that's required:

```csharp
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
var mongoClient = new MongoClient(clientSettings);
```

In the sample program, we've also added some options to the `DiagnosticsActivityEventSubscriber` to illustrate how you
can configure the subscriber to only send certain events to OpenTelemetry.
