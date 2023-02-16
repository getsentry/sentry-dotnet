namespace Sentry;

/// <summary>
/// Sentry View Hierarchy attachment.
/// </summary>
public class ViewHierarchyAttachment : Attachment
{
    public ViewHierarchyAttachment(IAttachmentContent content) :
        base(AttachmentType.ViewHierarchy, content, "view-hierarchy.json", "application/json")
    { }
}
