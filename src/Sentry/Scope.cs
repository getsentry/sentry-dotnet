using System;
using System.Collections.Concurrent;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Scope data to be sent with the event
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

        /// <summary>
        /// Whether the <see cref="OnEvaluating"/> event has already fired.
        /// </summary>
        public bool HasEvaluated => _hasEvaluated;

        /// <summary>
        /// A list of exception processors
        /// </summary>
        internal ConcurrentBag<ISentryEventExceptionProcessor> ExceptionProcessors { get; set; }
            = new ConcurrentBag<ISentryEventExceptionProcessor>();

        /// <summary>
        /// A list of event processors
        /// </summary>
        internal ConcurrentBag<ISentryEventProcessor> EventProcessors { get; set; }
            = new ConcurrentBag<ISentryEventProcessor>();

        /// <summary>
        /// An event that fires when the scope evaluates
        /// </summary>
        /// <remarks>
        /// This allows registering an event handler that is invoked in case
        /// an event is about to be sent to Sentry. If an event is never sent,
        /// this event is never fired and the resources spared.
        /// It also allows registration at an early stage of the processing
        /// but execution at a later time, when more data is available.
        /// </remarks>
        /// <see cref="Evaluate"/>
        public event EventHandler OnEvaluating;

        /// <summary>
        /// Creates a scope with the specified options
        /// </summary>
        /// <param name="options"></param>
        public Scope(SentryOptions options)
            : this(options ?? new SentryOptions(), true)
        {
        }

        // For testing. Should explicitly require SentryOptions
        internal Scope()
            : this(new SentryOptions(), true)
        { }

        internal Scope(SentryOptions options, bool addMainProcessor)
        : base(options)
        {
            Options = options;
        }

        public Scope Clone()
        {
            var clone = new Scope(Options, false);
            this.Apply(clone);

            if (EventProcessors != null)
            {
                clone.EventProcessors = new ConcurrentBag<ISentryEventProcessor>(EventProcessors);
            }

            if (ExceptionProcessors != null)
            {
                clone.ExceptionProcessors = new ConcurrentBag<ISentryEventExceptionProcessor>(ExceptionProcessors);
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
