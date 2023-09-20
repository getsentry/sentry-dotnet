namespace Sentry;

/// <summary>
/// A hint that can be provided when capturing a <see cref="SentryEvent"/> or when adding a <see cref="Breadcrumb"/>.
/// Hints can be used to filter or modify events, transactions, or breadcrumbs before they are sent to Sentry.
/// </summary>
public class Hint
{
    private readonly SentryOptions? _options;
    private readonly List<Attachment> _attachments = new();
    private Dictionary<string, object?>? _items;

    /// <summary>
    /// Creates a new instance of <see cref="Hint"/>.
    /// </summary>
    public Hint() : this(SentrySdk.CurrentHub.GetSentryOptions())
    {
    }

    internal Hint(SentryOptions? options)
    {
        _options = options;
    }

    /// <summary>
    /// Creates a new hint containing a single item.
    /// </summary>
    /// <param name="key">The key of the hint item.</param>
    /// <param name="value">The value of the hint item.</param>
    public Hint(string key, object? value)
        : this()
    {
        Items[key] = value;
    }

    /// <summary>
    /// Attachments added to the Hint.
    /// </summary>
    /// <remarks>
    /// This collection represents all of the attachments that will be sent to Sentry with the corresponding event.
    /// You can add or remove attachments from this collection as needed.
    /// </remarks>
    public ICollection<Attachment> Attachments => _attachments;

    /// <summary>
    /// A dictionary of arbitrary items provided with the Hint.
    /// </summary>
    /// <remarks>
    /// These are not sent to Sentry, but rather they are available during processing, such as when using
    /// BeforeSend and others.
    /// </remarks>
    public IDictionary<string, object?> Items => _items ??= new Dictionary<string, object?>();

    /// <summary>
    /// The Java SDK has some logic so that certain Hint types do not copy attachments from the Scope.
    /// This provides a location that allows us to do the same in the .NET SDK in the future.
    /// </summary>
    /// <param name="scope">The <see cref="Scope"/> that the attachments should be copied from</param>
    internal void AddAttachmentsFromScope(Scope scope) => _attachments.AddRange(scope.Attachments);

    /// <summary>
    /// Creates a new Hint with one or more attachments.
    /// </summary>
    /// <param name="filePath">The path to the file to attach.</param>
    /// <param name="type">The type of attachment.</param>
    /// <param name="contentType">The content type of the attachment.</param>
    public void AddAttachment(
        string filePath,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null)
    {
        if (_options is not null)
        {
            _attachments.Add(
                new Attachment(
                    type,
                    new FileAttachmentContent(filePath, _options.UseAsyncFileIO),
                    Path.GetFileName(filePath),
                    contentType));
        }
    }

    /// <summary>
    /// Creates a new Hint with one or more attachments.
    /// </summary>
    /// <param name="attachments">The attachment(s) to add.</param>
    /// <returns>A Hint having the attachment(s).</returns>
    public static Hint WithAttachments(params Attachment[] attachments) => WithAttachments(attachments.AsEnumerable());

    /// <summary>
    /// Creates a new Hint with attachments.
    /// </summary>
    /// <param name="attachments">The attachments to add.</param>
    /// <returns>A Hint having the attachments.</returns>
    public static Hint WithAttachments(IEnumerable<Attachment> attachments)
    {
        var hint = new Hint();
        hint._attachments.AddRange(attachments);
        return hint;
    }
}
