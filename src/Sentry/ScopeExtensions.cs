using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Scope extensions.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ScopeExtensions
{
    /// <summary>
    /// Invokes all event processor providers available.
    /// </summary>
    /// <param name="scope">The Scope which holds the processor providers.</param>
    public static IEnumerable<ISentryEventProcessor> GetAllEventProcessors(this Scope scope)
    {
        foreach (var processor in scope.Options.GetAllEventProcessors())
        {
            yield return processor;
        }

        foreach (var processor in scope.EventProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Invokes all transaction processor providers available.
    /// </summary>
    /// <param name="scope">The Scope which holds the processor providers.</param>
    public static IEnumerable<ISentryTransactionProcessor> GetAllTransactionProcessors(this Scope scope)
    {
        foreach (var processor in scope.Options.GetAllTransactionProcessors())
        {
            yield return processor;
        }

        foreach (var processor in scope.TransactionProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Invokes all exception processor providers available.
    /// </summary>
    /// <param name="scope">The Scope which holds the processor providers.</param>
    public static IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors(this Scope scope)
    {
        foreach (var processor in scope.Options.GetAllExceptionProcessors())
        {
            yield return processor;
        }

        foreach (var processor in scope.ExceptionProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Add an exception processor.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processor">The exception processor.</param>
    public static void AddExceptionProcessor(this Scope scope, ISentryEventExceptionProcessor processor)
        => scope.ExceptionProcessors.Add(processor);

    /// <summary>
    /// Add the exception processors.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processors">The exception processors.</param>
    public static void AddExceptionProcessors(this Scope scope, IEnumerable<ISentryEventExceptionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            scope.ExceptionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processor">The event processor.</param>
    public static void AddEventProcessor(this Scope scope, ISentryEventProcessor processor)
        => scope.EventProcessors.Add(processor);

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processor">The event processor.</param>
    public static void AddEventProcessor(this Scope scope, Func<SentryEvent, SentryEvent> processor)
        => scope.AddEventProcessor(new DelegateEventProcessor(processor));

    /// <summary>
    /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processors">The event processors.</param>
    public static void AddEventProcessors(this Scope scope, IEnumerable<ISentryEventProcessor> processors)
    {
        foreach (var processor in processors)
        {
            scope.EventProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processor">The transaction processor.</param>
    public static void AddTransactionProcessor(this Scope scope, ISentryTransactionProcessor processor)
        => scope.TransactionProcessors.Add(processor);

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processor">The transaction processor.</param>
    public static void AddTransactionProcessor(this Scope scope, Func<SentryTransaction, SentryTransaction?> processor)
        => scope.AddTransactionProcessor(new DelegateTransactionProcessor(processor));

    /// <summary>
    /// Adds transaction processors which are invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="scope">The Scope to hold the processor.</param>
    /// <param name="processors">The transaction processors.</param>
    public static void AddTransactionProcessors(this Scope scope, IEnumerable<ISentryTransactionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            scope.TransactionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    /// <remarks>
    /// Note: the stream must be seekable.
    /// </remarks>
    public static void AddAttachment(
        this Scope scope,
        Stream stream,
        string fileName,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null)
    {
        var length = stream.TryGetLength();
        if (length is null)
        {
            scope.Options.LogWarning(
                "Cannot evaluate the size of attachment '{0}' because the stream is not seekable.",
                fileName);

            return;
        }

        // TODO: Envelope spec allows the last item to not have a length.
        // So if we make sure there's only 1 item without length, we can support it.
        scope.AddAttachment(
            new SentryAttachment(
                type,
                new StreamAttachmentContent(stream),
                fileName,
                contentType));
    }

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    public static void AddAttachment(
        this Scope scope,
        byte[] data,
        string fileName,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null) =>
        scope.AddAttachment(
            new SentryAttachment(
                type,
                new ByteAttachmentContent(data),
                fileName,
                contentType));

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    public static void AddAttachment(
        this Scope scope,
        string filePath,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null) =>
        scope.AddAttachment(
            new SentryAttachment(
                type,
                new FileAttachmentContent(filePath, scope.Options.UseAsyncFileIO),
                Path.GetFileName(filePath),
                contentType));

    /// <summary>
    /// Gets the last opened span.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <returns>The last span not finished or null.</returns>
    internal static ISpan? LastCreatedSpan(this Scope scope)
        => scope.Transaction?.Spans.LastOrDefault(s => !s.IsFinished);
}
