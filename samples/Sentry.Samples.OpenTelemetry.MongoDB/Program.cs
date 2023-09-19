using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;

SentrySdk.Init(options =>
{
    // options.Dsn = "... Your DSN ...";
    options.Debug = true;
    options.TracesSampleRate = 1.0; // <-- Set the sample rate to 100% (in production you'd configure this to be lower)
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

using var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .ConfigureResource(resource => resource.AddService("sentry-mongo-sample"))
    .AddMongoDBInstrumentation() // <-- Adds the MongoDB OTel datasource
    .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    .Build();

/*
 * The following configuration for the MongoClient ensures diagnostic information gets generated for OpenTelemetry.
 */
var connectionString = "mongodb://localhost:27017"; // <-- Replace with your connection string
var clientSettings = MongoClientSettings.FromConnectionString(connectionString);
var instrumentationOptions = new InstrumentationOptions
{
    ShouldStartActivity = @event => "fruit".Equals(@event.GetCollectionName()) // <-- Optionally filter diagnostic information by collection name
};
clientSettings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber(instrumentationOptions));
var mongoClient = new MongoClient(clientSettings);

/*
 * Start a sentry transaction... OTel diagnostics will be captured and sent to Sentry in this transaction
 */
// var transaction = SentrySdk.StartTransaction("Program Main", "function");
// SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

/*
 * This is just some nonsense to demonstrate a few MongoDB operations... the main purpose of which is to generate
 * diagnostic information that will be captured by OpenTelemetry and sent to Sentry.
 */
var db = mongoClient.GetDatabase("sentryMongoSample");
var collection = db.GetCollection<BsonDocument>("fruit");

var filter = Builders<BsonDocument>.Filter.Eq("_id", "1");

var update = Builders<BsonDocument>.Update
    .Set("name", "Red Apple")
    .Set("color", "Red");

var options = new FindOneAndUpdateOptions<BsonDocument>
{
    IsUpsert = true
};

collection.FindOneAndUpdate(filter, update, options);

var document = collection.Find(filter).First();
Console.WriteLine(document);

/*
 * Finally, finish the transaction and send it to Sentry
 */
// transaction.Finish();
