using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Sentry client used to send events to Sentry.
    /// </summary>
    /// <remarks>
    /// This client captures events by queueing those to its
    /// internal background worker which sends events to Sentry.
    /// </remarks>
    /// <inheritdoc cref="ISentryClient" />
    /// <inheritdoc cref="IDisposable" />
    public class SentryClient : ISentryClient, IDisposable
    {
        private volatile bool _disposed;
        private readonly SentryOptions _options;

        private readonly Lazy<Random> _random = new Lazy<Random>(() => new Random(), LazyThreadSafetyMode.PublicationOnly);
        internal Random Random => _random.Value;

        // Internal for testing.
        internal IBackgroundWorker Worker { get; }

        /// <summary>
        /// Whether the client is enabled.
        /// </summary>
        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <summary>
        /// Creates a new instance of <see cref="SentryClient"/>.
        /// </summary>
        /// <param name="options">The configuration for this client.</param>
        public SentryClient(SentryOptions options)
            : this(options, null) { }

        internal SentryClient(
            SentryOptions options,
            IBackgroundWorker? worker)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            options.SetupLogging(); // Only relevant if this client wasn't created as a result of calling Init

            if (worker == null)
            {
                var composer = new SdkComposer(options);
                Worker = composer.CreateBackgroundWorker();
            }
            else
            {
                options.DiagnosticLogger?.LogDebug("Worker of type {0} was provided via Options.", worker.GetType().Name);
                Worker = worker;
            }
        }

        /// <summary>
        /// Queues the event to be sent to Sentry.
        /// </summary>
        /// <remarks>
        /// An optional scope, if provided, will be applied to the event.
        /// </remarks>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="scope">The optional scope to augment the event with.</param>
        /// <inheritdoc />
        public SentryId CaptureEvent(SentryEvent? @event, Scope? scope = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SentryClient));
            }

            if (@event == null)
            {
                return SentryId.Empty;
            }

            try
            {
                return DoSendEvent(@event, scope);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("An error occurred when capturing the event {0}.", e, @event.EventId);
                return SentryId.Empty;
            }
        }

        /// <summary>
        /// Flushes events asynchronously.
        /// </summary>
        /// <param name="timeout">How long to wait for flush to finish.</param>
        /// <returns>A task to await for the flush operation.</returns>
        public Task FlushAsync(TimeSpan timeout) => Worker.FlushAsync(timeout);

        // TODO: this method needs to be refactored, it's really hard to analyze nullability
        private SentryId DoSendEvent(SentryEvent @event, Scope? scope)
        {
            if (_options.SampleRate != null)
            {
                if (Random.NextDouble() > _options.SampleRate.Value)
                {
                    _options.DiagnosticLogger?.LogDebug("Event sampled.");
                    return SentryId.Empty;
                }
            }
            if (@event.Exception != null && _options.ExceptionFilters?.Length > 0)
            {
                if (_options.ExceptionFilters.Any(f => f.Filter(@event.Exception)))
                {
                    _options.DiagnosticLogger?.LogInfo(
                        "Event with exception of type '{0}' was dropped by an exception filter.", @event.Exception.GetType());
                    return SentryId.Empty;
                }
            }
            scope ??= new Scope(_options);

            _options.DiagnosticLogger?.LogInfo("Capturing event.");

            // Evaluate and copy before invoking the callback
            scope.Evaluate();
            scope.Apply(@event);

            if (scope.Level != null)
            {
                // Level on scope takes precedence over the one on event
                _options.DiagnosticLogger?.LogInfo("Overriding level set on event '{0}' with level set on scope '{1}'.", @event.Level, scope.Level);
                @event.Level = scope.Level;
            }

            if (@event.Exception != null)
            {
                // Depends on Options instead of the processors to allow application adding new processors
                // after the SDK is initialized. Useful for example once a DI container is up
                foreach (var processor in scope.GetAllExceptionProcessors())
                {
                    processor.Process(@event.Exception, @event);
                }
            }

            SentryEvent? processedEvent = @event;

            foreach (var processor in scope.GetAllEventProcessors())
            {
                processedEvent = processor.Process(processedEvent);
                if (processedEvent == null)
                {
                    _options.DiagnosticLogger?.LogInfo("Event dropped by processor {0}", processor.GetType().Name);
                    return SentryId.Empty;
                }
            }

            processedEvent = BeforeSend(processedEvent);
            if (processedEvent == null) // Rejected event
            {
                _options.DiagnosticLogger?.LogInfo("Event dropped by BeforeSend callback.");
                return SentryId.Empty;
            }

            if (Worker.EnqueueEvent(processedEvent))
            {
                _options.DiagnosticLogger?.LogDebug("Event queued up.");
                return processedEvent.EventId;
            }

            _options.DiagnosticLogger?.LogWarning("The attempt to queue the event failed. Items in queue: {0}",
                Worker.QueuedItems);

            return SentryId.Empty;
        }

        private SentryEvent? BeforeSend(SentryEvent? @event)
        {
            if (_options.BeforeSend == null)
            {
                return @event;
            }

            _options.DiagnosticLogger?.LogDebug("Calling the BeforeSend callback");
            try
            {
                @event = _options.BeforeSend?.Invoke(@event!);
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.LogError("The BeforeSend callback threw an exception. It will be added as breadcrumb and continue.", e);

                @event?.AddBreadcrumb(
                    "BeforeSend callback failed.",
                    category: "SentryClient",
                    data: new Dictionary<string, string>
                    {
                        {"message", e.Message},
                        {"stackTrace", e.StackTrace}
                    },
                    level: BreadcrumbLevel.Error);
            }

            return @event;
        }

        /// <summary>
        /// Disposes this client
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            _options.DiagnosticLogger?.LogDebug("Disposing SentryClient.");

            if (_disposed)
            {
                return;
            }

            _disposed = true;

            // Worker should empty it's queue until SentryOptions.ShutdownTimeout
            (Worker as IDisposable)?.Dispose();
        }
    }
}
