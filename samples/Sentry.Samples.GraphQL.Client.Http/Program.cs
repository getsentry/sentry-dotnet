/*
 * This sample demonstrates using Sentry to capture traces and exceptions from a GraphQL over HTTP client.
 * It assumes the Sentry.Samples.GraphQL.Server is running on http://localhost:5051
 * (see `/Samples/Sentry.Samples.GraphQL.Server/Properties/launchSettings.json`)
 */

using System.Text.Json;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;

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
        options.HttpMessageHandler = new SentryGraphQLHttpMessageHandler(); // <-- Configure GraphQL use Sentry Message Handler
    },
    new SystemTextJsonSerializer()
    );

char input = ' ';
do
{
    Console.WriteLine("Select a query to send:");
    Console.WriteLine("1. Add a note");
    Console.WriteLine("2. Get all notes");
    Console.WriteLine("3. Generate a GraphQL Error");
    Console.WriteLine("0. Exit Note Master 3000");
    input = Console.ReadKey().KeyChar;
    Console.WriteLine();
    switch (input)
    {
        case '0':
            Console.WriteLine("Bye!");
            break;
        case '1':
            await CreateNewNote();
            break;
        case '2':
            await GetAllNotes();
            break;
        case '3':
            await CreateError();
            break;
        default:
            Console.WriteLine("Invalid selection.");
            break;
    }
} while (input != '0');
transaction.Finish(SpanStatus.Ok);

async Task CreateError()
{
    // var query = new GraphQLRequest(@"{ test { id } }");
    var query = new GraphQLRequest
    {
        Query = @"mutation fakeMutation($note:NoteInput!) { playNote(note: $note) { id } }",
        OperationName = "fakeMutation",
        Variables = new
        {
            note = new
            {
                message = "This should put a spanner in the works"
            }
        }
    };
    var response = await graphClient!.SendQueryAsync<NotesResult>(query);
    var result = JsonSerializer.Serialize(response);
    Console.WriteLine(result);
}

async Task CreateNewNote()
{
    Console.WriteLine("What do you want the note to  say?");
    var message = Console.ReadLine();
    var mutation = new GraphQLRequest
    {
        Query = @"mutation addANote($note:NoteInput!) { createNote(note: $note) {id message } }",
        OperationName = "addANote",
        Variables = new
        {
            note = new
            {
                message = message
            }
        }
    };
    var newNote = await graphClient!.SendQueryAsync<CreateNoteResult>(mutation);
    Console.WriteLine("Note added:");
    Console.WriteLine("{0,3} | {1}", newNote.Data.CreateNote.Id, newNote.Data.CreateNote.Message);
}

async Task GetAllNotes()
{
    var query = new GraphQLRequest(@"query getAllNotes { notes { id, message } }");
    var allNotesResponse = await graphClient.SendQueryAsync<NotesResult>(query);
    Console.WriteLine();
    foreach (var note in allNotesResponse.Data.Notes)
    {
        Console.WriteLine("{0,3} | {1}", note.Id, note.Message);
    }
    Console.WriteLine();
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

public class CreateNoteResult
{
    public Note CreateNote { get; set; } = new();
}
