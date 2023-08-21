using GraphQL;
using GraphQL.Types;

namespace Sentry.Samples.GraphQL.Server.Notes;

public sealed class NotesMutation : ObjectGraphType
{
    public NotesMutation(NotesData data)
    {
        Field<NoteType>("createNote")
            .Argument<NonNullGraphType<NoteInputType>>("note")
            .Resolve(context =>
            {
                var note = context.GetArgument<Note>("note");
                return data.AddNote(note);
            });
    }
}

public class NoteInputType : InputObjectGraphType
{
    public NoteInputType()
    {
        Name = "NoteInput";
        Field<NonNullGraphType<StringGraphType>>("message");
    }
}
