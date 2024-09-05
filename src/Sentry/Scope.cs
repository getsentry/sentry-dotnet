using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Scope data to be sent with the event.
/// </summary>
/// <remarks>
/// Scope data is sent together with any event captured
/// during the lifetime of the scope.
/// </remarks>
public class Scope : IEventLike
{
    internal SentryOptions Options { get; }

    internal bool Locked { get; set; }

    private readonly object _lastEventIdSync = new();
    private SentryId _lastEventId;

    internal SentryId LastEventId
    {
        get
        {
            lock (_lastEventIdSync)
            {
                return _lastEventId;
            }
        }
        set
        {
            lock (_lastEventIdSync)
            {
                _lastEventId = value;
            }
        }
    }

    private readonly object _evaluationSync = new();
    private volatile bool _hasEvaluated;

    /// <summary>
    /// Whether the <see cref="OnEvaluating"/> event has already fired.
    /// </summary>
    internal bool HasEvaluated => _hasEvaluated;

    private readonly Lazy<ConcurrentBag<ISentryEventExceptionProcessor>> _lazyExceptionProcessors =
        new(LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// A list of exception processors.
    /// </summary>
    internal ConcurrentBag<ISentryEventExceptionProcessor> ExceptionProcessors => _lazyExceptionProcessors.Value;

    private readonly Lazy<ConcurrentBag<ISentryEventProcessor>> _lazyEventProcessors =
        new(LazyThreadSafetyMode.PublicationOnly);

    private readonly Lazy<ConcurrentBag<ISentryTransactionProcessor>> _lazyTransactionProcessors =
        new(LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// A list of event processors.
    /// </summary>
    internal ConcurrentBag<ISentryEventProcessor> EventProcessors => _lazyEventProcessors.Value;

    /// <summary>
    /// A list of event processors.
    /// </summary>
    internal ConcurrentBag<ISentryTransactionProcessor> TransactionProcessors => _lazyTransactionProcessors.Value;

    /// <summary>
    /// An event that fires when the scope evaluates.
    /// </summary>
    /// <remarks>
    /// This allows registering an event handler that is invoked in case
    /// an event is about to be sent to Sentry. If an event is never sent,
    /// this event is never fired and the resources spared.
    /// It also allows registration at an early stage of the processing
    /// but execution at a later time, when more data is available.
    /// </remarks>
    /// <see cref="Evaluate"/>
    internal event EventHandler<Scope>? OnEvaluating;

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    private SentryRequest? _request;

    /// <inheritdoc />
    public SentryRequest Request
    {
        get => _request ??= new SentryRequest();
        set => _request = value;
    }

    private readonly SentryContexts _contexts = new();

    /// <inheritdoc />
    public SentryContexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    // Internal for testing.
    internal Action<SentryUser?> UserChanged => user =>
    {
        if (Options.EnableScopeSync &&
            Options.ScopeObserver is { } observer)
        {
            observer.SetUser(user);
        }
    };

    private SentryUser? _user;

    /// <inheritdoc />
    public SentryUser User
    {
        get => _user ??= new SentryUser
        {
            PropertyChanged = UserChanged
        };
        set
        {
            if (_user != value)
            {
                _user = value;
                if (_user is not null)
                {
                    _user.PropertyChanged = UserChanged;
                }

                UserChanged.Invoke(_user);
            }
        }
    }

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    /// <inheritdoc />
    public string? Environment { get; set; }

    // TransactionName is kept for legacy purposes because
    // SentryEvent still makes use of it.
    // It should be possible to set the transaction name
    // without starting a fully fledged transaction.
    // Consequently, Transaction.Name and TransactionName must
    // be kept in sync as much as possible.

    private string? _fallbackTransactionName;

    /// <inheritdoc />
    public string? TransactionName
    {
        get => Transaction?.Name ?? _fallbackTransactionName;
        set
        {
            // Set the fallback regardless, so that the variable is always kept up to date
            _fallbackTransactionName = value;

            // If a transaction has been started, overwrite its name
            if (Transaction is { } transaction)
            {
                // Null name is not allowed in a transaction, but
                // allowed on `scope.TransactionName` because it's optional.
                // As a workaround, we coerce null into empty string.
                // Context: https://github.com/getsentry/develop/issues/246#issuecomment-762274438

                transaction.Name = !string.IsNullOrWhiteSpace(value)
                    ? value
                    : string.Empty;
            }
        }
    }

    /// <summary>
    /// <para>
    /// Most of the properties on the Scope should have the same affinity as the Scope... For example, when using a
    /// GlobalScopeStackContainer, anything you store on the scope will be applied to all events that get sent to Sentry
    /// (no matter which thread they are sent from).
    /// </para>
    /// <para>
    /// Transactions are an exception, however. We don't want spans from threads created on the UI thread to be added as
    /// children of Transactions/Spans that get created on the background thread, or vice versa. As such,
    /// Scope.Transaction is always stored as an AsyncLocal, regardless of the ScopeStackContainer implementation.
    /// </para>
    /// <para>
    /// See https://github.com/getsentry/sentry-dotnet/issues/3590 for more information.
    /// </para>
    /// </summary>
    private readonly AsyncLocal<ITransactionTracer?> _transaction = new();

    /// <summary>
    /// The current Transaction
    /// </summary>
    public ITransactionTracer? Transaction
    {
        get
        {
            _transactionLock.EnterReadLock();
            try
            {
                return _transaction.Value;
            }
            finally
            {
                _transactionLock.ExitReadLock();
            }
        }
        set
        {
            _transactionLock.EnterWriteLock();
            try
            {
                _transaction.Value = value;
            }
            finally
            {
                _transactionLock.ExitWriteLock();
            }
        }
    }

    internal SentryPropagationContext PropagationContext { get; set; }

    internal SessionUpdate? SessionUpdate { get; set; }

    /// <inheritdoc />
    public SdkVersion Sdk { get; } = new();

    /// <inheritdoc />
    public IReadOnlyList<string> Fingerprint { get; set; } = Array.Empty<string>();

#if NETSTANDARD2_0 || NETFRAMEWORK
    private ConcurrentQueue<Breadcrumb> _breadcrumbs = new();
#else
    private readonly ConcurrentQueue<Breadcrumb> _breadcrumbs = new();
#endif

    /// <inheritdoc />
    public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

    private readonly ConcurrentDictionary<string, object?> _extra = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object?> Extra => _extra;

    private readonly ConcurrentDictionary<string, string> _tags = new();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Tags => _tags;

#if NETSTANDARD2_0 || NETFRAMEWORK
    private ConcurrentBag<SentryAttachment> _attachments = new();
#else
    private readonly ConcurrentBag<SentryAttachment> _attachments = new();
#endif

    /// <summary>
    /// Attachments.
    /// </summary>
    public IReadOnlyCollection<SentryAttachment> Attachments => _attachments;

    /// <summary>
    /// Creates a scope with the specified options.
    /// </summary>
    public Scope(SentryOptions? options)
        : this(options, null)
    {
    }

    internal Scope(SentryOptions? options, SentryPropagationContext? propagationContext)
    {
        Options = options ?? new SentryOptions();
        PropagationContext = new SentryPropagationContext(propagationContext);
    }

    // For testing. Should explicitly require SentryOptions.
    internal Scope()
        : this(new SentryOptions())
    {
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) => AddBreadcrumb(breadcrumb, new SentryHint());

    /// <summary>
    /// Adds a breadcrumb with a hint.
    /// </summary>
    /// <param name="breadcrumb">The breadcrumb</param>
    /// <param name="hint">A hint for use in the BeforeBreadcrumb callback</param>
    public void AddBreadcrumb(Breadcrumb breadcrumb, SentryHint hint)
    {
        if (Options.BeforeBreadcrumbInternal is { } beforeBreadcrumb)
        {
            hint.AddAttachmentsFromScope(this);

            if (beforeBreadcrumb(breadcrumb, hint) is { } processedBreadcrumb)
            {
                breadcrumb = processedBreadcrumb;
            }
            else
            {
                // Callback returned null, which means the breadcrumb should be dropped
                return;
            }
        }

        if (Options.MaxBreadcrumbs <= 0)
        {
            //Always drop the breadcrumb.
            return;
        }

        if (Breadcrumbs.Count - Options.MaxBreadcrumbs + 1 > 0)
        {
            _breadcrumbs.TryDequeue(out _);
        }

        _breadcrumbs.Enqueue(breadcrumb);
        if (Options.EnableScopeSync)
        {
            Options.ScopeObserver?.AddBreadcrumb(breadcrumb);
        }
    }

    /// <inheritdoc />
    public void SetExtra(string key, object? value)
    {
        _extra[key] = value;
        if (Options.EnableScopeSync)
        {
            Options.ScopeObserver?.SetExtra(key, value);
        }
    }

    /// <inheritdoc />
    public void SetTag(string key, string value)
    {
        if (Options.TagFilters.MatchesSubstringOrRegex(key))
        {
            return;
        }

        _tags[key] = value;
        if (Options.EnableScopeSync)
        {
            Options.ScopeObserver?.SetTag(key, value);
        }
    }

    /// <inheritdoc />
    public void UnsetTag(string key)
    {
        _tags.TryRemove(key, out _);
        if (Options.EnableScopeSync)
        {
            Options.ScopeObserver?.UnsetTag(key);
        }
    }

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    public void AddAttachment(SentryAttachment attachment) => _attachments.Add(attachment);

    /// <summary>
    /// Resets all the properties and collections within the scope to their default values.
    /// </summary>
    public void Clear()
    {
        Level = default;
        Request = new();
        Contexts.Clear();
        User = new();
        Release = default;
        Distribution = default;
        Environment = default;
        TransactionName = default;
        Transaction = default;
        Fingerprint = Array.Empty<string>();
        ClearBreadcrumbs();
        _extra.Clear();
        _tags.Clear();
        ClearAttachments();
        PropagationContext = new();
    }

    /// <summary>
    /// Clear all Attachments.
    /// </summary>
    public void ClearAttachments()
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        Interlocked.Exchange(ref _attachments, new());
#else
        _attachments.Clear();
#endif
    }

    /// <summary>
    /// Removes all Breadcrumbs from the scope.
    /// </summary>
    public void ClearBreadcrumbs()
    {
#if NETSTANDARD2_0 || NETFRAMEWORK
        // No Clear method on ConcurrentQueue for these target frameworks
        Interlocked.Exchange(ref _breadcrumbs, new());
#else
        _breadcrumbs.Clear();
#endif
    }

    /// <summary>
    /// Applies the data from this scope to another event-like object.
    /// </summary>
    /// <param name="other">The scope to copy data to.</param>
    /// <remarks>
    /// Applies the data of 'from' into 'to'.
    /// If data in 'from' is null, 'to' is unmodified.
    /// Conflicting keys are not overriden.
    /// This is a shallow copy.
    /// </remarks>
    public void Apply(IEventLike other)
    {
        // Not to throw on code that ignores nullability warnings.
        if (other.IsNull())
        {
            return;
        }

        // Fingerprint isn't combined. It's absolute.
        // One set explicitly on target (i.e: event)
        // takes precedence and is not overwritten
        if (!other.Fingerprint.Any() && Fingerprint.Any())
        {
            other.Fingerprint = Fingerprint;
        }

        foreach (var breadcrumb in Breadcrumbs)
        {
            other.AddBreadcrumb(breadcrumb);
        }

        foreach (var (key, value) in Extra)
        {
            if (!other.Extra.ContainsKey(key))
            {
                other.SetExtra(key, value);
            }
        }

        foreach (var (key, value) in Tags)
        {
            if (!other.Tags.ContainsKey(key))
            {
                other.SetTag(key, value);
            }
        }

        Contexts.CopyTo(other.Contexts);
        Request.CopyTo(other.Request);
        User.CopyTo(other.User);

        other.Release ??= Release;
        other.Distribution ??= Distribution;
        other.Environment ??= Environment;
        other.TransactionName ??= TransactionName;
        other.Level ??= Level;

        if (Sdk.Name is not null && Sdk.Version is not null)
        {
            other.Sdk.Name = Sdk.Name;
            other.Sdk.Version = Sdk.Version;
        }

        foreach (var package in Sdk.InternalPackages)
        {
            other.Sdk.AddPackage(package);
        }
    }

    /// <summary>
    /// Applies data from one scope to another.
    /// </summary>
    public void Apply(Scope other)
    {
        // Not to throw on code that ignores nullability warnings.
        if (other.IsNull())
        {
            return;
        }

        Apply((IEventLike)other);

        other.Transaction ??= Transaction;
        other.SessionUpdate ??= SessionUpdate;

        foreach (var attachment in Attachments)
        {
            other.AddAttachment(attachment);
        }
    }

    /// <summary>
    /// Applies the state object into the scope.
    /// </summary>
    /// <param name="state">The state object to apply.</param>
    public void Apply(object state) => Options.SentryScopeStateProcessor.Apply(this, state);

    /// <summary>
    /// Clones the current <see cref="Scope"/>.
    /// </summary>
    public Scope Clone()
    {
        var clone = new Scope(Options, PropagationContext)
        {
            OnEvaluating = OnEvaluating
        };

        Apply(clone);

        foreach (var processor in EventProcessors)
        {
            clone.EventProcessors.Add(processor);
        }

        foreach (var processor in TransactionProcessors)
        {
            clone.TransactionProcessors.Add(processor);
        }

        foreach (var processor in ExceptionProcessors)
        {
            clone.ExceptionProcessors.Add(processor);
        }

        return clone;
    }

    internal void Evaluate()
    {
        if (_hasEvaluated)
        {
            return;
        }

        lock (_evaluationSync)
        {
            if (_hasEvaluated)
            {
                return;
            }

            try
            {
                OnEvaluating?.Invoke(this, this);
            }
            catch (Exception ex)
            {
                Options.DiagnosticLogger?.LogError(ex, "Failed invoking event handler.");
            }
            finally
            {
                _hasEvaluated = true;
            }
        }
    }

    private ISpan? _span;

    /// <summary>
    /// Gets or sets the active span, or <c>null</c> if none available.
    /// </summary>
    /// <remarks>
    /// If a span has been set on this property, it will become the active span until it is finished.
    /// Otherwise, the active span is the latest unfinished span on the transaction, presuming a transaction
    /// was set on the scope via the <see cref="Transaction"/> property.
    /// </remarks>
    public ISpan? Span
    {
        get
        {
            if (_span?.IsFinished is false)
            {
                return _span;
            }

            return Transaction?.GetLastActiveSpan() ?? Transaction;
        }
        set => _span = value;
    }

    /// <summary>
    /// Invokes all event processor providers available.
    /// </summary>
    public IEnumerable<ISentryEventProcessor> GetAllEventProcessors()
    {
        foreach (var processor in Options.GetAllEventProcessors())
        {
            yield return processor;
        }

        foreach (var processor in EventProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Invokes all transaction processor providers available.
    /// </summary>
    public IEnumerable<ISentryTransactionProcessor> GetAllTransactionProcessors()
    {
        foreach (var processor in Options.GetAllTransactionProcessors())
        {
            yield return processor;
        }

        foreach (var processor in TransactionProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Invokes all exception processor providers available.
    /// </summary>
    public IEnumerable<ISentryEventExceptionProcessor> GetAllExceptionProcessors()
    {
        foreach (var processor in Options.GetAllExceptionProcessors())
        {
            yield return processor;
        }

        foreach (var processor in ExceptionProcessors)
        {
            yield return processor;
        }
    }

    /// <summary>
    /// Add an exception processor.
    /// </summary>
    /// <param name="processor">The exception processor.</param>
    public void AddExceptionProcessor(ISentryEventExceptionProcessor processor)
        => ExceptionProcessors.Add(processor);

    /// <summary>
    /// Add the exception processors.
    /// </summary>
    /// <param name="processors">The exception processors.</param>
    public void AddExceptionProcessors(IEnumerable<ISentryEventExceptionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            ExceptionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    public void AddEventProcessor(ISentryEventProcessor processor)
        => EventProcessors.Add(processor);

    /// <summary>
    /// Adds an event processor which is invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processor">The event processor.</param>
    public void AddEventProcessor(Func<SentryEvent, SentryEvent> processor)
        => AddEventProcessor(new DelegateEventProcessor(processor));

    /// <summary>
    /// Adds event processors which are invoked when creating a <see cref="SentryEvent"/>.
    /// </summary>
    /// <param name="processors">The event processors.</param>
    public void AddEventProcessors(IEnumerable<ISentryEventProcessor> processors)
    {
        foreach (var processor in processors)
        {
            EventProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processor">The transaction processor.</param>
    public void AddTransactionProcessor(ISentryTransactionProcessor processor)
        => TransactionProcessors.Add(processor);

    /// <summary>
    /// Adds an transaction processor which is invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processor">The transaction processor.</param>
    public void AddTransactionProcessor(Func<SentryTransaction, SentryTransaction?> processor)
        => AddTransactionProcessor(new DelegateTransactionProcessor(processor));

    /// <summary>
    /// Adds transaction processors which are invoked when creating a <see cref="SentryTransaction"/>.
    /// </summary>
    /// <param name="processors">The transaction processors.</param>
    public void AddTransactionProcessors(IEnumerable<ISentryTransactionProcessor> processors)
    {
        foreach (var processor in processors)
        {
            TransactionProcessors.Add(processor);
        }
    }

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    /// <remarks>
    /// Note: the stream must be seekable.
    /// </remarks>
    public void AddAttachment(
        Stream stream,
        string fileName,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null)
    {
        var length = stream.TryGetLength();
        if (length is null)
        {
            Options.LogWarning(
                "Cannot evaluate the size of attachment '{0}' because the stream is not seekable.",
                fileName);

            return;
        }

        // TODO: Envelope spec allows the last item to not have a length.
        // So if we make sure there's only 1 item without length, we can support it.
        AddAttachment(
            new SentryAttachment(
                type,
                new StreamAttachmentContent(stream),
                fileName,
                contentType));
    }

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    public void AddAttachment(
        byte[] data,
        string fileName,
        AttachmentType type = AttachmentType.Default,
        string? contentType = null) =>
        AddAttachment(
            new SentryAttachment(
                type,
                new ByteAttachmentContent(data),
                fileName,
                contentType));

    /// <summary>
    /// Adds an attachment.
    /// </summary>
    public void AddAttachment(string filePath, AttachmentType type = AttachmentType.Default, string? contentType = null)
        => AddAttachment(
            new SentryAttachment(
                type,
                new FileAttachmentContent(filePath, Options.UseAsyncFileIO),
                Path.GetFileName(filePath),
                contentType));

    /// <summary>
    /// We need this lock to prevent a potential race condition in <see cref="ResetTransaction"/>.
    /// </summary>
    private readonly ReaderWriterLockSlim _transactionLock = new();

    internal void ResetTransaction(ITransactionTracer? expectedCurrentTransaction)
    {
        _transactionLock.EnterWriteLock();
        try
        {
            if (_transaction.Value == expectedCurrentTransaction)
            {
                _transaction.Value = null;
            }
        }
        finally
        {
            _transactionLock.ExitWriteLock();
        }
    }
}
