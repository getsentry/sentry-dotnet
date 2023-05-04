using System;
using System.Collections.Generic;
using System.Net.Mail;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// A hint that can be provided when capturing a <see cref="SentryEvent"/> or adding a <see cref="Breadcrumb"/>.
/// Hints can be used to filter or modify events or breadcrumbs before they are sent to Sentry.
/// </summary>
public class Hint : ICollection, IEnumerable<KeyValuePair<string, object?>>
{
    private readonly Dictionary<string, object?> _internalStorage = new();
    private readonly List<Attachment> _attachments = new();

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
        _internalStorage[key] = value;
    }

    /// <summary>
    /// Gets or sets additional values to be provided with the hint
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>The value with the specified key or null if none exist.</returns>
    public object? this[string key]
    {
        get => _internalStorage.GetValueOrDefault(key);
        set => _internalStorage[key] = value;
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
    /// Attachments added to the Hint.
    /// </summary>
    public ICollection<Attachment> Attachments => _attachments;

    /// <summary>
    /// Clears any values stored in <see cref="this[string]"/>
    /// </summary>
    public void Clear() => _internalStorage.Clear();

    /// <summary>
    /// Checks if the specified key exists
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>True if the key exists. False otherwise.</returns>
    public bool ContainsKey(string key) => _internalStorage.ContainsKey(key);

    /// <inheritdoc />
    public void CopyTo(Array array, int index) => ((ICollection)_internalStorage).CopyTo(array, index);

    /// <inheritdoc />
    public int Count => _internalStorage.Count;

    IEnumerator IEnumerable.GetEnumerator() => _internalStorage.GetEnumerator();

    /// <inheritdoc />
    public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        => ((IEnumerable<KeyValuePair<string, object?>>)_internalStorage).GetEnumerator();

    /// <summary>
    /// Gets the value with the specified key as type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">They expected value type</typeparam>
    /// <param name="key">The key</param>
    /// <returns>A value of type <typeparamref name="T"/> if one exists with the specified key or null otherwise.</returns>
    public T? GetValue<T>(string key) where T : class? => (this[key] is T typedHintValue) ? typedHintValue : null;

    /// <inheritdoc />
    public bool IsSynchronized => ((ICollection)_internalStorage).IsSynchronized;

    /// <summary>
    /// Remves the value with the specified key
    /// </summary>
    /// <param name="key"></param>
    public void Remove(string key) => _internalStorage.Remove(key);

    /// <summary>
    /// Gets or sets a Screenshot for the Hint
    /// </summary>
    public Attachment? Screenshot { get; set; }

    /// <inheritdoc />
    public object SyncRoot => ((ICollection)_internalStorage).SyncRoot;

    /// <summary>
    /// Gets or sets a ViewHierarchy for the Hint
    /// </summary>
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
