# Overview

This sample demonstrates how a MongoDB commands can be automatically instrumented using Sentry's OpenTelemetry package.

There are a few steps to get this working.

## 1. Install NuGet packages

For this sample, we've used the following NuGet packages
- `MongoDB.Driver`
- `MongoDB.Driver.Core.Extensions.OpenTelemetry`
- `OpenTelemetry`
- `Sentry`
- `Sentry.OpenTelemetry`

## 2. Configure Sentry OpenTelemetry Tracing

In the SentryOptions builder:

```csharp
    options.TracesSampleRate = 1.0; // <-- Set the sample rate to 100% (in production you'd configure this to be lower)
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
```

## 3. Configure OpenTelemetry Trace to collect MongoDB diagnostics

On the OpenTelemetry TraceProviderBuilder:

```csharp
Sdk.CreateTracerProviderBuilder()
    .AddMongoDBInstrumentation() // <-- Adds the MongoDB OTel datasource
```

## 4. ConfigureOpenTelemetry to send traces to Sentry

On the OpenTelemetry TraceProviderBuilder:

```csharp
Sdk.CreateTracerProviderBuilder()
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
```

## 5. Enable MongoDB diagnostic activity

Technically, this is all that's required:

```csharp
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
var mongoClient = new MongoClient(clientSettings);
```

In the sample program, we've also added some options to the `DiagnosticsActivityEventSubscriber` to illustrate how you
can configure the subscriber to only send certain events to OpenTelemetry.
