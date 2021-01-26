using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

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

        /// <inheritdoc />
        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SentryClient));
            }

            if (userFeedback.EventId.Equals(SentryId.Empty))
            {
                // Ignore the user feedback if EventId is empty
                _options.DiagnosticLogger?.LogWarning("User feedback dropped due to empty id.");
                return;
            }

            CaptureEnvelope(Envelope.FromUserFeedback(userFeedback));
        }

        /// <inheritdoc />
        public void CaptureTransaction(ITransaction transaction)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SentryClient));
            }

            if (transaction.SpanId.Equals(SpanId.Empty))
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Transaction dropped due to empty id."
                );

                return;
            }

            if (string.IsNullOrWhiteSpace(transaction.Name) ||
                string.IsNullOrWhiteSpace(transaction.Operation))
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Transaction discarded due to one or more required fields missing."
                );

                return;
            }

            // Unfinished transaction can only happen if the user calls this method instead of
            // transaction.Finish().
            // We still send these transactions over, but warn the user not to do it.
            if (!transaction.IsFinished)
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Capturing a transaction which has not been finished. " +
                    "Please call transaction.Finish() instead of hub.CaptureTransaction(transaction) " +
                    "to properly finalize the transaction and send it to Sentry."
                );
            }

            // Sampling decision MUST have been made at this point
            Debug.Assert(
                transaction.IsSampled != null,
                "Attempt to capture transaction without sampling decision."
            );

            if (transaction.IsSampled != true)
            {
                _options.DiagnosticLogger?.LogDebug(
                    "Transaction dropped by sampling."
                );

                return;
            }

            CaptureEnvelope(Envelope.FromTransaction(transaction));
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
                if (SynchronizedRandom.NextDouble() > _options.SampleRate.Value)
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

            return CaptureEnvelope(Envelope.FromEvent(processedEvent, scope.Attachments))
                ? processedEvent.EventId
                : SentryId.Empty;
        }

        /// <summary>
        /// Capture an envelope and queue it.
        /// </summary>
        /// <param name="envelope">The envelope.</param>
        /// <returns>true if the enveloped was queued, false otherwise.</returns>
        private bool CaptureEnvelope(Envelope envelope)
        {
            if (Worker.EnqueueEnvelope(envelope))
            {
                _options.DiagnosticLogger?.LogDebug("Envelope queued up.");
                return true;
            }

            _options.DiagnosticLogger?.LogWarning(
                "The attempt to queue the event failed. Items in queue: {0}",
                Worker.QueuedItems
            );

            return false;
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
                var data = new Dictionary<string, string>
                {
                    {"message", e.Message}
                };
                if(e.StackTrace is not null)
                {
                    data.Add("stackTrace", e.StackTrace);
                }
                @event?.AddBreadcrumb(
                    "BeforeSend callback failed.",
                    category: "SentryClient",
                    data: data,
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
