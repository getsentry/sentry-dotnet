using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
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
        private readonly SentryOptions _options;
        private readonly RandomValuesFactory _randomValuesFactory;

        internal IBackgroundWorker Worker { get; }
        internal SentryOptions Options => _options;

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
            : this(options, null, null) { }

        internal SentryClient(
            SentryOptions options,
            RandomValuesFactory? randomValuesFactory)
            : this(options, null, randomValuesFactory)
        {
        }

        internal SentryClient(
            SentryOptions options,
            IBackgroundWorker? worker,
            RandomValuesFactory? randomValuesFactory = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _randomValuesFactory = randomValuesFactory ?? new SynchronizedRandomValuesFactory();

            options.SetupLogging(); // Only relevant if this client wasn't created as a result of calling Init

            if (worker == null)
            {
                var composer = new SdkComposer(options);
                Worker = composer.CreateBackgroundWorker();
            }
            else
            {
                options.LogDebug("Worker of type {0} was provided via Options.", worker.GetType().Name);
                Worker = worker;
            }
        }

        /// <inheritdoc />
        public SentryId CaptureEvent(SentryEvent? @event, Scope? scope = null)
        {
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
                _options.LogError("An error occurred when capturing the event {0}.", e, @event.EventId);
                return SentryId.Empty;
            }
        }

        /// <inheritdoc />
        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            if (userFeedback.EventId.Equals(SentryId.Empty))
            {
                // Ignore the user feedback if EventId is empty
                _options.LogWarning("User feedback dropped due to empty id.");
                return;
            }

            CaptureEnvelope(Envelope.FromUserFeedback(userFeedback));
        }

        /// <inheritdoc />
        public void CaptureTransaction(Transaction transaction)
        {
            if (transaction.SpanId.Equals(SpanId.Empty))
            {
                _options.LogWarning("Transaction dropped due to empty id.");
                return;
            }

            if (string.IsNullOrWhiteSpace(transaction.Name) ||
                string.IsNullOrWhiteSpace(transaction.Operation))
            {
                _options.LogWarning("Transaction discarded due to one or more required fields missing.");
                return;
            }

            // Unfinished transaction can only happen if the user calls this method instead of
            // transaction.Finish().
            // We still send these transactions over, but warn the user not to do it.
            if (!transaction.IsFinished)
            {
                _options.LogWarning("Capturing a transaction which has not been finished. " +
                                    "Please call transaction.Finish() instead of hub.CaptureTransaction(transaction) " +
                                    "to properly finalize the transaction and send it to Sentry.");
            }


            // Sampling decision MUST have been made at this point
            Debug.Assert(transaction.IsSampled != null,
                "Attempt to capture transaction without sampling decision.");


            if (transaction.IsSampled != true)
            {
                _options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.SampleRate, DataCategory.Transaction);
                _options.LogDebug("Transaction dropped by sampling.");
                return;
            }

            CaptureEnvelope(Envelope.FromTransaction(transaction));
        }

        /// <inheritdoc />
        public void CaptureSession(SessionUpdate sessionUpdate)
        {
            CaptureEnvelope(Envelope.FromSession(sessionUpdate));
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
                if (!_randomValuesFactory.NextBool(_options.SampleRate.Value))
                {
                    _options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.SampleRate, DataCategory.Error);
                    _options.LogDebug("Event sampled.");
                    return SentryId.Empty;
                }
            }

            var filteredExceptions = ApplyExceptionFilters(@event.Exception);
            if (filteredExceptions?.Count > 0)
            {
                _options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
                _options.LogInfo("Event was dropped by one or more exception filters for exception(s): {0}",
                    string.Join(", ", filteredExceptions.Select(e => e.GetType()).Distinct()));
                return SentryId.Empty;
            }

            scope ??= new Scope(_options);

            _options.LogInfo("Capturing event.");

            // Evaluate and copy before invoking the callback
            scope.Evaluate();
            scope.Apply(@event);

            if (scope.Level != null)
            {
                // Level on scope takes precedence over the one on event
                _options.LogInfo("Overriding level set on event '{0}' with level set on scope '{1}'.", @event.Level, scope.Level);
                @event.Level = scope.Level;
            }

            if (@event.Exception != null)
            {
                // Depends on Options instead of the processors to allow application adding new processors
                // after the SDK is initialized. Useful for example once a DI container is up
                foreach (var processor in scope.GetAllExceptionProcessors())
                {
                    processor.Process(@event.Exception, @event);

                    // NOTE: Exception processors can't drop events, but exception filters (above) can.
                }
            }

            var processedEvent = @event;

            foreach (var processor in scope.GetAllEventProcessors())
            {
                processedEvent = processor.Process(processedEvent);
                if (processedEvent == null)
                {
                    _options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.EventProcessor, DataCategory.Error);
                    _options.LogInfo("Event dropped by processor {0}", processor.GetType().Name);
                    return SentryId.Empty;
                }
            }

            processedEvent = BeforeSend(processedEvent);
            if (processedEvent == null) // Rejected event
            {
                _options.ClientReportRecorder.RecordDiscardedEvent(DiscardReason.BeforeSend, DataCategory.Error);
                _options.LogInfo("Event dropped by BeforeSend callback.");
                return SentryId.Empty;
            }

            return CaptureEnvelope(Envelope.FromEvent(processedEvent, _options.DiagnosticLogger, scope.Attachments, scope.SessionUpdate))
                ? processedEvent.EventId
                : SentryId.Empty;
        }

        private IReadOnlyCollection<Exception>? ApplyExceptionFilters(Exception? exception)
        {
            var filters = _options.ExceptionFilters;
            if (exception == null || filters == null || filters.Count == 0)
            {
                // There was nothing to filter.
                return null;
            }

            if (filters.Any(f => f.Filter(exception)))
            {
                // The event should be filtered based on the given exception
                return new[] {exception};
            }

            if (exception is AggregateException aggregate &&
                aggregate.InnerExceptions.All(e => ApplyExceptionFilters(e) != null))
            {
                // All inner exceptions of the aggregate matched a filter, so the event should be filtered.
                // Note that _options.KeepAggregateException is not relevant here.  Even if we want to keep aggregate
                // exceptions, we would still never send one if all of its children are supposed to be filtered.
                return aggregate.InnerExceptions;
            }

            // The event should not be filtered.
            return null;
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
                _options.LogInfo("Envelope queued up: '{0}'", envelope.TryGetEventId(_options.DiagnosticLogger));
                return true;
            }

            _options.LogWarning(
                "The attempt to queue the event failed. Items in queue: {0}",
                Worker.QueuedItems);

            return false;
        }

        private SentryEvent? BeforeSend(SentryEvent? @event)
        {
            if (_options.BeforeSend == null)
            {
                return @event;
            }

            _options.LogDebug("Calling the BeforeSend callback");
            try
            {
                @event = _options.BeforeSend?.Invoke(@event!);
            }
            catch (Exception e)
            {
                // Attempt to demystify exceptions before adding them as breadcrumbs.
                e.Demystify();

                _options.LogError("The BeforeSend callback threw an exception. It will be added as breadcrumb and continue.", e);
                var data = new Dictionary<string, string>
                {
                    {"message", e.Message}
                };
                if (e.StackTrace is not null)
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
        [Obsolete("Sentry client should not be explicitly disposed of. This method will be removed in version 4.")]
        public void Dispose()
        {
            _options.LogDebug("Flushing SentryClient.");

            // Worker should empty it's queue until SentryOptions.ShutdownTimeout
            Worker.FlushAsync(_options.ShutdownTimeout).GetAwaiter().GetResult();
        }
    }
}
