using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry
{
    public class SentryClient : ISentryClient, IDisposable
    {
        private volatile bool _disposed;
        private readonly SentryOptions _options;
        // Internal for testing
        internal IBackgroundWorker Worker { get; }

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
                Worker = composer.CreateBackgroundWorker();
            }
            else
            {
                Worker = worker;
            }
        }

        public Guid CaptureEvent(SentryEvent @event, Scope scope = null)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(BackgroundWorker));
            }

            if (@event == null)
            {
                return Guid.Empty;
            }

            // Evaluate and copy before invoking the callback
            scope?.Evaluate();
            scope?.CopyTo(@event);

            @event = BeforeSend(@event);
            if (@event == null) // Rejected event
            {
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
