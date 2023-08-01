using GraphQL.Types;

namespace Sentry.Samples.GraphQL.Server.Notes;

public sealed class NotesQuery : ObjectGraphType
{
    private static int NextId = 0;
    private readonly Lazy<List<Note>> _notes = new (
        () => new List<Note>
        {
            new() { Id = NextId++, Message = "Hello World!" },
            new() { Id = NextId++, Message = "Hello World! How are you?" }
        }
    );

    public NotesQuery()
    {
        Field<ListGraphType<NoteType>>("notes").Resolve(context => _notes.Value);
    }
}
