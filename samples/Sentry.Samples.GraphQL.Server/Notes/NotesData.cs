namespace Sentry.Samples.GraphQL.Server.Notes;

public class NotesData
{
    private static int NextId = 0;
    private readonly ICollection<Note> _notes = new List<Note>()
    {
        new() { Id = NextId++, Message = "Hello World!" },
        new() { Id = NextId++, Message = "Hello World! How are you?" }
    };

    public ICollection<Note> GetAll() => _notes;

    public Task<Note?> GetNoteByIdAsync(int id)
    {
        return Task.FromResult(_notes.FirstOrDefault(n => n.Id == id));
    }

    public Note AddNote(Note note)
    {
        note.Id = NextId++;
        _notes.Add(note);
        return note;
    }
}
