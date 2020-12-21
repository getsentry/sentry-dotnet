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
        public string? Environment { get; set; }

        /// <summary>
        /// The name of the transaction in which there was an event.
        /// </summary>
        /// <remarks>
        /// A transaction should only be defined when it can be well defined.
        /// On a Web framework, for example, a transaction is the route template
        /// rather than the actual request path. That is so GET /user/10 and /user/20
        /// (which have route template /user/{id}) are identified as the same transaction.
        /// </remarks>
        public string? TransactionName { get; set; }

        /// <inheritdoc />
        public Transaction? Transaction { get; set; }

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

        public void SetExtra(string key, object? value) => _extra[key] = value;

        public void SetTag(string key, string value) => _tags[key] = value;

        public void AddAttachment(Attachment attachment) => _attachments.Add(attachment);

        /// <summary>
        /// Applies the data from this scope to the other.
        /// </summary>
        /// <param name="other">The scope to copy data to.</param>
        /// <remarks>
        /// Applies the data of 'from' into 'to'.
        /// If data in 'from' is null, 'to' is unmodified.
        /// Conflicting keys are not overriden.
        /// This is a shallow copy.
        /// </remarks>
        public void Apply(Scope other)
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

            other.Environment ??= Environment;
            other.Transaction ??= Transaction;
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
                catch (Exception e)
                {
                    AddBreadcrumb(
                        new Breadcrumb(
                            message: "Failed invoking event handler: " + e,
                            level: BreadcrumbLevel.Error
                        )
                    );
                }
                finally
                {
                    _hasEvaluated = true;
                }
            }
        }
    }
}
