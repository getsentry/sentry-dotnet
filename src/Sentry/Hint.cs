using System;
using System.Collections.Generic;
using System.Net.Mail;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// A hint for the <see cref="SentryClient"/> to decide whether an event should be sent or cached. It also
/// holds data that should be injected into the event.
/// </summary>
public class Hint : ICollection, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _internalStorage = new();
    private readonly List<Attachment> _attachments = new();

    public object? this[string name]
    {
        get => _internalStorage.GetValueOrDefault(name);
        set => _internalStorage[name] = value;
    }

    internal void AddAttachmentsInternal(IEnumerable<Attachment> attachments)
    {
        if (attachments is not null)
        {
            _attachments.AddRange(attachments);
        }
    }

    public void AddAttachments(params Attachment[] attachments) => AddAttachmentsInternal(attachments);

    public void AddAttachments(ICollection<Attachment> attachments) => AddAttachmentsInternal(attachments);

    public ICollection<Attachment> Attachments => _attachments;

    public void Clear() => _internalStorage.Clear();

    public bool ContainsKey(string key) => _internalStorage.ContainsKey(key);

    public void CopyTo(Array array, int index) => ((ICollection)_internalStorage).CopyTo(array, index);

    public int Count => _internalStorage.Count;

    IEnumerator IEnumerable.GetEnumerator() => _internalStorage.GetEnumerator();

    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<string, object?>>)_internalStorage).GetEnumerator();

    public T? GetAs<T>(string name) where T : class? => (this[name] is T typedHintValue) ? typedHintValue : null;

    public bool IsSynchronized => ((ICollection)_internalStorage).IsSynchronized;

    public void Remove(string name) => _internalStorage.Remove(name);

    public Attachment? Screenshot { get; set; }

    public object SyncRoot => ((ICollection)_internalStorage).SyncRoot;

    public Attachment? ViewHierarchy { get; set; }

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
