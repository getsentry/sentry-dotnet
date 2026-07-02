/*
 * This sample demonstrates how MongoDB queries can be captured as Sentry spans using the
 * MongoDB.Driver's built-in OpenTelemetry instrumentation (available since driver version 3.7.0).
 *
 * The sample requires a MongoDB server. See the README.md alongside this sample for instructions
 * on how to start one using Docker.
 */

using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.OpenTelemetry.Exporter;

#if SENTRY_DSN_DEFINED_IN_ENV
var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
          ?? throw new InvalidOperationException("SENTRY_DSN environment variable is not set");
#else
// A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
// See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
var dsn = SamplesShared.Dsn;
#endif

var activitySource = new ActivitySource("Sentry.Samples.OpenTelemetry.MongoDB");

SentrySdk.Init(options =>
{
    options.Dsn = dsn;
    options.Debug = true;
    options.TracesSampleRate = 1.0;
    options.UseOtlp(); // <-- Configure Sentry to use OpenTelemetry trace information
});

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(activitySource.Name)
    .AddSource(MongoTelemetry.ActivitySourceName) // <-- Subscribe to the MongoDB driver's built-in instrumentation
    .AddSentryOtlpExporter(dsn) // <-- Configure OpenTelemetry to send traces to Sentry over OTLP
    .Build();

// MongoDB.Driver 3.7.0+ creates OpenTelemetry activities for every command out of the box.
// TracingOptions is only needed to tweak that behaviour - here we opt in to capturing the query
// text on each span (off by default), which shows up in Sentry as the `db.query.text` span data.
var mongoUri = Environment.GetEnvironmentVariable("MONGODB_URI") ?? "mongodb://localhost:27017";
var clientSettings = MongoClientSettings.FromConnectionString(mongoUri);
clientSettings.ServerSelectionTimeout = TimeSpan.FromSeconds(3); // <-- Fail fast if MongoDB isn't running
clientSettings.TracingOptions = new TracingOptions
{
    QueryTextMaxLength = 4096
};
var mongoClient = new MongoClient(clientSettings);

var db = mongoClient.GetDatabase("sentry_mongo_sample");
var fruit = db.GetCollection<BsonDocument>("fruit");

try
{
    // Everything within this activity gets sent to Sentry as a single transaction, containing one
    // span for each MongoDB command.
    using (activitySource.StartActivity("Fruit Salad"))
    {
        await fruit.InsertManyAsync([
            new BsonDocument { { "name", "Apple" }, { "color", "Red" } },
            new BsonDocument { { "name", "Banana" }, { "color", "Yellow" } },
            new BsonDocument { { "name", "Cherry" }, { "color", "Red" } },
            new BsonDocument { { "name", "Kiwi" }, { "color", "Green" } }
        ]);

        var redFruit = await fruit.Find(Builders<BsonDocument>.Filter.Eq("color", "Red")).ToListAsync();
        Console.WriteLine($"Found {redFruit.Count} red fruit: " +
                          string.Join(", ", redFruit.Select(f => f["name"].AsString)));

        await fruit.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("name", "Apple"),
            Builders<BsonDocument>.Update.Set("color", "Green"));

        var count = await fruit.CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("color", "Green"));
        Console.WriteLine($"There are now {count} green fruit.");

        // Drop the database so the sample can be run repeatedly
        await mongoClient.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName);
    }
}
catch (TimeoutException)
{
    Console.WriteLine($"""
        Could not connect to MongoDB at {mongoUri}
        Is MongoDB running? See the README.md for this sample for instructions on starting MongoDB using Docker.
        """);
}
