using GraphQL.Types;

namespace Sentry.Samples.GraphQL.Server.Notes;

public sealed class NotesQuery : ObjectGraphType
{
    public NotesQuery(NotesData data)
    {
        Field<ListGraphType<NoteType>>("notes").Resolve(context => data.GetAll());
    }
}
