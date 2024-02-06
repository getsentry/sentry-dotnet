namespace Sentry;

/// <summary>
/// Sentry View Hierarchy attachment.
/// </summary>
public class ViewHierarchyAttachment : SentryAttachment
{
    /// <summary>
    /// Initializes an instance of <see cref="ViewHierarchyAttachment"/>.
    /// </summary>
    /// /// <param name="content">The view hierarchy attachment</param>
    public ViewHierarchyAttachment(IAttachmentContent content) :
        base(AttachmentType.ViewHierarchy, content, "view-hierarchy.json", "application/json")
    { }
}
