using GraphQL.Types;

namespace Sentry.Samples.GraphQL.Server.Notes;

public class NotesSchema : Schema
{
    public NotesSchema(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        Query = serviceProvider.GetRequiredService<NotesQuery>();
        Mutation = serviceProvider.GetRequiredService<NotesMutation>();
    }
}
