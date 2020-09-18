using System;
using System.Collections.Concurrent;
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
    /// <inheritdoc />
    public class Scope : BaseScope
    {
        private volatile bool _hasEvaluated;
        private readonly object _evaluationSync = new object();

        internal bool Locked { get; set; }
        internal SentryOptions Options { get; }

        private readonly object _lastEventIdSync = new object();

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

        /// <summary>
        /// Whether the <see cref="OnEvaluating"/> event has already fired.
        /// </summary>
        internal bool HasEvaluated => _hasEvaluated;

        private readonly Lazy<ConcurrentBag<ISentryEventExceptionProcessor>> _lazyExceptionProcessors =
            new Lazy<ConcurrentBag<ISentryEventExceptionProcessor>>(LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// A list of exception processors.
        /// </summary>
        internal ConcurrentBag<ISentryEventExceptionProcessor> ExceptionProcessors => _lazyExceptionProcessors.Value;

        private readonly Lazy<ConcurrentBag<ISentryEventProcessor>> _lazyEventProcessors =
            new Lazy<ConcurrentBag<ISentryEventProcessor>>(LazyThreadSafetyMode.PublicationOnly);

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

        /// <summary>
        /// Creates a scope with the specified options.
        /// </summary>
        public Scope(SentryOptions? options)
            : base(options)
        {
            Options = options ?? new SentryOptions();
        }

        // For testing. Should explicitly require SentryOptions.
        internal Scope()
            : this(new SentryOptions())
        {
        }

        /// <summary>
        /// Clones the current <see cref="Scope"/>.
        /// </summary>
        public Scope Clone()
        {
            var clone = new Scope(Options);
            this.Apply(clone);

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
                    this.AddBreadcrumb("Failed invoking event handler: " + e,
                        level: BreadcrumbLevel.Error);
                }
                finally
                {
                    _hasEvaluated = true;
                }
            }
        }
    }
}
