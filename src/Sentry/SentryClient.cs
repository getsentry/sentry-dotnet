using System;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry
{
    public class SentryClient : ISentryClient, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly IBackgroundWorker _worker;

        private readonly Guid _failureId = Guid.Empty;

        public bool IsEnabled => true;

        public SentryClient(SentryOptions options)
            : this(options, null) { }

        internal SentryClient(
            SentryOptions options,
            IBackgroundWorker worker)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (worker == null)
            {
                var composer = new SdkComposer(options);
                _worker = composer.CreateBackgroundWorker();
            }
        }

        public Guid CaptureEvent(SentryEvent @event, Scope scope = null)
        {
            var id = _failureId;
            scope?.Evaluate();

            scope?.CopyTo(@event);

            if (_options.BeforeSend != null)
            {
                @event = _options.BeforeSend?.Invoke(@event);
                if (@event == null) // Rejected event
                {
                    return id;
                }
            }

            if (_worker.EnqueueEvent(@event))
            {
                id = @event.EventId;
            }
            else
            {
                // TODO: Notify error handler
                Debug.WriteLine("Failed to enqueue event. Current queue depth: " + _worker.QueuedItems);
            }

            return id;
        }

        public void Dispose()
        {
            // Worker should empty it's queue until SentryOptions.ShutdownTimeout
            (_worker as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}
