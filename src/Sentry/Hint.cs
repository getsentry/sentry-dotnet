namespace Sentry;

/// <summary>
/// A hint that can be provided when capturing a <see cref="SentryEvent"/> or adding a <see cref="Breadcrumb"/>.
/// Hints can be used to filter or modify events or breadcrumbs before they are sent to Sentry.
/// </summary>
public class Hint
{
    private readonly List<Attachment> _attachments = new();
    private readonly Dictionary<string, object?> _items = new();

    /// <summary>
    /// Creates a new instance of <see cref="Hint"/>.
    /// </summary>
    public Hint()
    {
    }

    /// <summary>
    /// Creates a new hint with a single key/value pair.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    public Hint(string key, object? value)
        : this()
    {
        _items[key] = value;
    }

    internal void AddAttachmentsInternal(IEnumerable<Attachment> attachments)
    {
        if (attachments is not null)
        {
            _attachments.AddRange(attachments);
        }
    }

    /// <summary>
    /// Adds one or more attachments to the Hint.
    /// </summary>
    /// <param name="attachments"></param>
    public void AddAttachments(params Attachment[] attachments) => AddAttachmentsInternal(attachments);

    /// <summary>
    /// Adds multiple attachments to the Hint.
    /// </summary>
    /// <param name="attachments"></param>
    public void AddAttachments(IEnumerable<Attachment> attachments) => AddAttachmentsInternal(attachments);

    /// <summary>
    /// The Java SDK has some logic so that certain Hint types do not copy attachments from the Scope. This
    /// allows us to do the same in the .NET SDK in the future.
    /// </summary>
    /// <param name="scope">The <see cref="Scope"/> that the attachments should be copied from</param>
    internal void AddScopeAttachments(Scope scope) => AddAttachmentsInternal(scope.Attachments);

    /// <summary>
    /// Attachments added to the Hint.
    /// </summary>
    public ICollection<Attachment> Attachments => _attachments;

    /// <summary>
    /// Data provided with the Hint.
    /// </summary>
    public IDictionary<string, object?> Items => _items;

    /// <summary>
    /// Creates a new Hint with one or more attachments.
    /// </summary>
    /// <param name="attachment"></param>
    /// <returns></returns>
    public static Hint WithAttachments(params Attachment[] attachment) => Hint.WithAttachments(attachment.ToList());

    /// <summary>
    /// Creates a new Hint with attachments.
    /// </summary>
    /// <param name="attachments"></param>
    /// <returns></returns>
    public static Hint WithAttachments(ICollection<Attachment> attachments)
    {
        var hint = new Hint();
        hint.AddAttachments(attachments);
        return hint;
    }
}
