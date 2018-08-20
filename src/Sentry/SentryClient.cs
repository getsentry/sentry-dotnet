using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Sentry client used to send events to Sentry
    /// </summary>
    /// <remarks>
    /// This client captures events by queueing those to its
    /// internal background worker which sends events to Sentry
    /// </remarks>
    /// <inheritdoc cref="ISentryClient" />
    /// <inheritdoc cref="IDisposable" />
    public class SentryClient : ISentryClient, IDisposable
    {
        private volatile bool _disposed;
        private readonly SentryOptions _options;

        private readonly Lazy<Random> _random = new Lazy<Random>(() => new Random(), LazyThreadSafetyMode.PublicationOnly);
        internal Random Random => _random.Value;

        // Internal for testing
        internal IBackgroundWorker Worker { get; }

        /// <inheritdoc />
        /// <summary>
        /// Whether the client is enabled
        /// </summary>
        public bool IsEnabled => true;

        /// <summary>
        /// Creates a new instance of <see cref="SentryClient"/>
        /// </summary>
        /// <param name="options">The configuration for this client.</param>
        public SentryClient(SentryOptions options)
            : this(options, null) { }

        internal SentryClient(
            SentryOptions options,
            IBackgroundWorker worker)
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
                Worker = worker;
            }
        }

        /// <summary>
        /// Queues the event to be sent to Sentry
        /// </summary>
        /// <remarks>
        /// An optional scope, if provided, will be applied to the event.
        /// </remarks>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="scope">The optional scope to augment the event with.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Guid CaptureEvent(SentryEvent @event, Scope scope = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SentryClient));
            }

            if (@event == null)
            {
                return Guid.Empty;
            }

            if (_options.SampleRate is float sample)
            {
                if (Random.NextDouble() > sample)
                {
                    // TODO: Log here event dropped due to sampling
                    return Guid.Empty;
                }
            }

            // Evaluate and copy before invoking the callback
            scope?.Evaluate();
            scope?.Apply(@event);

            foreach (var processor in _options.GetAllEventProcessors())
            {
                @event = processor.Process(@event);
                if (@event == null)
                {
                    // TODO: Log here which processor dropped it
                    return Guid.Empty;
                }
            }

            @event = BeforeSend(@event);
            if (@event == null) // Rejected event
            {
                // TODO: Log BeforeSend callback dropped it
                return Guid.Empty;
            }

            if (Worker.EnqueueEvent(@event))
            {
                return @event.EventId;
            }

            // TODO: Notify error handler
            Debug.WriteLine("Failed to enqueue event. Current queue depth: " + Worker.QueuedItems);
            return Guid.Empty;
        }

        private SentryEvent BeforeSend(SentryEvent @event)
        {
            if (_options.BeforeSend == null)
            {
                return @event;
            }

            try
            {
                @event = _options.BeforeSend?.Invoke(@event);
            }
            catch (Exception e)
            {
                @event.AddBreadcrumb(
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
