using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry
{
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

        /// <summary>
        /// A list of event processors.
        /// </summary>
        internal ConcurrentBag<ISentryEventProcessor> EventProcessors => _lazyEventProcessors.Value;

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
        internal event EventHandler? OnEvaluating;

        /// <inheritdoc />
        public SentryLevel? Level { get; set; }

        private Request? _request;

        /// <inheritdoc />
        public Request Request
        {
            get => _request ??= new Request();
            set => _request = value;
        }

        private Contexts? _contexts;

        /// <inheritdoc />
        public Contexts Contexts
        {
            get => _contexts ??= new Contexts();
            set => _contexts = value;
        }

        private User? _user;

        /// <inheritdoc />
        public User User
        {
            get => _user ??= new User();
            set => _user = value;
        }

        /// <inheritdoc />
        public string? Release { get; set; }

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
        /// Transaction.
        /// </summary>
        public ITransaction? Transaction { get; set; }

        /// <inheritdoc />
        public SdkVersion Sdk { get; } = new();

        /// <inheritdoc />
        public IReadOnlyList<string> Fingerprint { get; set; } = Array.Empty<string>();

        private readonly ConcurrentQueue<Breadcrumb> _breadcrumbs = new();

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

        private readonly ConcurrentDictionary<string, object?> _extra = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extra;

        private readonly ConcurrentDictionary<string, string> _tags = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags;

        private readonly ConcurrentBag<Attachment> _attachments = new();

        /// <summary>
        /// Attachments.
        /// </summary>
        public IReadOnlyCollection<Attachment> Attachments => _attachments;

        /// <summary>
        /// Creates a scope with the specified options.
        /// </summary>
        public Scope(SentryOptions? options)
        {
            Options = options ?? new SentryOptions();
        }

        // For testing. Should explicitly require SentryOptions.
        internal Scope()
            : this(new SentryOptions())
        {
        }

        /// <inheritdoc />
        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            if (Options.BeforeBreadcrumb is { } beforeBreadcrumb)
            {
                if (beforeBreadcrumb(breadcrumb) is { } processedBreadcrumb)
                {
                    breadcrumb = processedBreadcrumb;
                }
                else
                {
                    // Callback returned null, which means the breadcrumb should be dropped
                    return;
                }
            }

            var overflow = Breadcrumbs.Count - Options.MaxBreadcrumbs + 1;

            if (overflow > 0)
            {
                _breadcrumbs.TryDequeue(out _);
            }

            _breadcrumbs.Enqueue(breadcrumb);
        }

        /// <inheritdoc />
        public void SetExtra(string key, object? value) => _extra[key] = value;

        /// <inheritdoc />
        public void SetTag(string key, string value) => _tags[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) => _tags.TryRemove(key, out _);

        /// <summary>
        /// Adds an attachment.
        /// </summary>
        public void AddAttachment(Attachment attachment) => _attachments.Add(attachment);

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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (other is null)
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
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (other is null)
            {
                return;
            }

            Apply((IEventLike)other);

            other.Transaction ??= Transaction;

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
            var clone = new Scope(Options);
            Apply(clone);

            foreach (var processor in EventProcessors)
            {
                clone.EventProcessors.Add(processor);
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
                    OnEvaluating?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Options.DiagnosticLogger?.LogError(
                        "Failed invoking event handler.",
                        ex
                    );
                }
                finally
                {
                    _hasEvaluated = true;
                }
            }
        }

        /// <summary>
        /// Gets the currently ongoing (not finished) span or <code>null</code> if none available.
        /// This relies on the transactions being manually set on the scope via <see cref="Transaction"/>.
        /// </summary>
        public ISpan? GetSpan() => Transaction?.GetLastActiveSpan() ?? Transaction;
    }
}
