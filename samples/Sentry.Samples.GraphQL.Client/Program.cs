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
using Sentry.GraphQl;

SentrySdk.Init(options =>
{
    // options.Dsn = "... Your DSN ...";
    options.CaptureFailedRequests = true;
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
        options.HttpMessageHandler = new SentryGraphQlHttpMessageHandler(); // <-- Configure GraphQL use Sentry Message Handler
    },
    new SystemTextJsonSerializer()
    );

var errorQuery = @"{ test { id } }";
var getAllNotes = @"query getAllNotes { notes { id, message } }";

Console.WriteLine("Select a query to send:");
Console.WriteLine("1. Get all notes");
Console.WriteLine("2. Generate a GraphQL Error");
// Console.WriteLine("3. Update a note");
var input = Console.ReadKey().KeyChar;
switch (input)
{
    case '1':
        var notesResponse = await graphClient.SendQueryAsync<NotesResult>(new GraphQLRequest(getAllNotes));
        PrintResponseAsJson(notesResponse);
        break;
    case '2':
        var errorResponse = await graphClient.SendQueryAsync<NotesResult>(new GraphQLRequest(errorQuery));
        PrintResponseAsJson(errorResponse);
        break;
    default:
        Console.WriteLine("Invalid selection.");
        break;
}

transaction.Finish(SpanStatus.Ok);

void PrintResponseAsJson(object response)
{
    var result = JsonSerializer.Serialize(response);
    Console.WriteLine(result);
}

public class Note
{
    public int Id { get; set; }
    public string? Message { get; set; }
}

public class NotesResult
{
    public List<Note> Notes { get; set; } = new();
}
