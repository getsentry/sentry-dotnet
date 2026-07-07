# Sentry.Samples.OpenTelemetry.MongoDB

This sample demonstrates how MongoDB queries can be captured as Sentry spans using OpenTelemetry.

[MongoDB.Driver](https://www.nuget.org/packages/MongoDB.Driver) 3.7.0 and later has
[built-in OpenTelemetry instrumentation](https://www.mongodb.com/docs/drivers/csharp/current/logging-and-monitoring/)
— no additional instrumentation package is required. The driver emits one activity per MongoDB
command from an `ActivitySource` named `MongoDB.Driver`, which this sample subscribes to and
exports to Sentry:

```csharp
using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(MongoTelemetry.ActivitySourceName) // "MongoDB.Driver"
    .AddSentryOtlpExporter(dsn)
    .Build();
```

By default the driver does not record the query text on spans. This sample opts in via
`TracingOptions` on the client settings, so each span includes the (truncated) command as
`db.query.text`:

```csharp
clientSettings.TracingOptions = new TracingOptions
{
    QueryTextMaxLength = 4096
};
```

## A note on PII

When you capture query text, the MongoDB driver records the **actual field values** sent in each
command (for example `{ "name": "Apple", "contributor": "Alice Johnson" }`) in the `db.query.text`
span attribute. If those values can contain PII, you need to scrub them.

Importantly, the Sentry OTLP exporter used by this sample sends spans **straight to Sentry**, without
running them through the SDK's `BeforeSend`/`BeforeSendTransaction` hooks — so you can't rely on those
to redact data here.

Your options are:
- **Don't capture query text at all** — leave `QueryTextMaxLength` at its default of `0`. The Queries
  module still works using the operation and collection name.
- **Redact client-side with an OpenTelemetry span processor** — registered *before*
  `AddSentryOtlpExporter` so it runs before spans are exported. This sample includes an example
  `RedactSensitiveMongoData`, which strips the `contributor` field out of `db.query.text`
  before it leaves the process. Adapt the field names to your own data.
- **Configure [server-side data scrubbing](https://docs.sentry.io/security-legal-pii/scrubbing/server-side-scrubbing/)**
  — note the default rules only catch known-sensitive patterns (passwords, tokens, card numbers), so
  for arbitrary field values you'll need to add explicit scrubbing rules.

## Prerequisites

### MongoDB

The sample expects a MongoDB server at `mongodb://localhost:27017`. The easiest way to get one
running is with Docker:

```shell
docker run --rm -d -p 27017:27017 --name sentry-mongo mongo:8
```

To check that it's up:

```shell
docker ps --filter name=sentry-mongo
```

And to stop it again once you're done (the `--rm` flag means the container is removed
automatically when stopped):

```shell
docker stop sentry-mongo
```

If your MongoDB server is running somewhere else, set the `MONGODB_URI` environment variable to
the appropriate connection string before running the sample.

### Sentry DSN

As with the other samples in this repository, you need a Sentry DSN. Either set the `SENTRY_DSN`
environment variable, or replace the placeholder in [samples/SamplesShared.cs](../SamplesShared.cs).
See https://docs.sentry.io/product/sentry-basics/dsn-explainer/ for details.

## Running the sample

```shell
dotnet run --project samples/Sentry.Samples.OpenTelemetry.MongoDB
```

The sample performs a few operations against a `sentry_mongo_sample` database (insert, find,
update, count and drop) inside a single transaction. In Sentry, look for the `Fruit Salad`
transaction in **Performance** — each MongoDB command shows up as a child span, with the query
text attached as `db.query.text`.
