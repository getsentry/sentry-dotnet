/*
 * This sample demonstrates using Sentry to capture traces and exceptions from a GraphQL client.
 * It assumes the Sentry.Samples.GraphQL.Server is running on http://localhost:5051
 * (see `/Samples/Sentry.Samples.GraphQL.Server/Properties/launchSettings.json`)
 */

using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using Sentry;

SentrySdk.Init(options =>
{
    // options.Dsn = "... Your DSN ...";
    options.SendDefaultPii = true;
    options.TracesSampleRate = 1.0;
    options.EnableTracing = true;
});

var transaction = SentrySdk.StartTransaction("Program Main", "function");
SentrySdk.ConfigureScope(scope => scope.Transaction = transaction);

var graphClient = new GraphQLHttpClient(
    options =>
    {
        options.EndPoint = new Uri("http://localhost:5051/graphql"); // Assumes Sentry.Samples.GraphQL.Server is running
        options.HttpMessageHandler = new SentryHttpMessageHandler(); // <-- Configure GraphQL use Sentry Message Handler
    },
    new SystemTextJsonSerializer()
    );
var notesRequest = new GraphQLRequest
{
    Query = @"
        getAllNotes {
          notes {
            id,
            message
          }
        }"
};

while(true)
{
    Console.WriteLine("Press any key to continue (or `q` to quit)");
    if (Console.ReadKey().KeyChar == 'q')
    {
        break;
    }

    var graphResponse = await graphClient.SendQueryAsync<NotesResult>(notesRequest);
    var result = JsonSerializer.Serialize(graphResponse.Data);
    Console.WriteLine(result);
}

transaction.Finish(SpanStatus.Ok);

public class Note
{
    public int Id { get; set; }
    public string? Message { get; set; }
}

public class NotesResult
{
    public List<Note> Notes { get; set; } = new();
}
