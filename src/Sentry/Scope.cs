using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;
using Sentry.Protocol;

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

    private Request? _request;

    /// <inheritdoc />
    public Request Request
    {
        get => _request ??= new Request();
        set => _request = value;
    }

    private readonly Contexts _contexts = new();

    /// <inheritdoc />
    public Contexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    // Internal for testing.
    internal Action<User?> UserChanged => user =>
    {
        if (Options.EnableScopeSync &&
            Options.ScopeObserver is { } observer)
        {
            observer.SetUser(user);
        }
    };

    private User? _user;

    /// <inheritdoc />
    public User User
    {
        get => _user ??= new User
        {
            PropertyChanged = UserChanged
        };
        set
        {
            _user = value;
            if (_user is not null)
            {
                _user.PropertyChanged = UserChanged;
            }

            UserChanged.Invoke(_user);
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

    private ITransaction? _transaction;

    /// <summary>
    /// Transaction.
    /// </summary>
    public ITransaction? Transaction
    {
        get => _transaction;
        set => _transaction = value;
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
    private ConcurrentBag<Attachment> _attachments = new();
#else
    private readonly ConcurrentBag<Attachment> _attachments = new();
#endif

    /// <summary>
    /// Attachments.
    /// </summary>
    public IReadOnlyCollection<Attachment> Attachments => _attachments;

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
    public void AddBreadcrumb(Breadcrumb breadcrumb) => AddBreadcrumb(breadcrumb, new Hint());

    /// <summary>
    /// Adds a breadcrumb with a hint.
    /// </summary>
    /// <param name="breadcrumb">The breadcrumb</param>
    /// <param name="hint">A hint for use in the BeforeBreadcrumb callback</param>
    public void AddBreadcrumb(Breadcrumb breadcrumb, Hint hint)
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
        if (Options.TagFilters.Any(x => x.IsMatch(key)))
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
    public void AddAttachment(Attachment attachment) => _attachments.Add(attachment);

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

    /// <summary>
    /// Obsolete.  Use the <see cref="Span"/> property instead.
    /// </summary>
    [Obsolete("Use the Span property instead.  This method will be removed in a future release.")]
    public ISpan? GetSpan() => Span;

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

    internal void ResetTransaction(ITransaction? expectedCurrentTransaction) =>
        Interlocked.CompareExchange(ref _transaction, null, expectedCurrentTransaction);
}
