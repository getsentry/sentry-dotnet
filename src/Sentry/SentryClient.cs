using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            // TODO: Apply scope to event
            var id = _failureId;
            try
            {
                // TODO: prepare event run on the worker thread
                @event = PrepareEvent(@event, scope);
                if (@event == null) // Rejected event
                {
                    return id;
                }

                if (_options.BeforeSend != null)
                {
                    @event = _options.BeforeSend?.Invoke(@event);
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
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString()); // TODO: logger
            }

            return id;
        }

        private static SentryEvent PrepareEvent(SentryEvent @event, Scope scope)
        {
            if (scope == null)
            {
                return @event;
            }

            // TODO: Consider multiple events being sent with the same scope:
            // Wherever this code will end up, it should evaluate only once
            if (scope.States != null)
            {
                var counter = 0;
                foreach (var state in scope.States)
                {
                    if (state is string scopeString)
                    {
                        counter++;
                        @event.SetTag("scope" + counter, scopeString);
                    }
                    else if (state is IEnumerable<KeyValuePair<string, string>> keyValStringString)
                    {
                        @event.SetTags(keyValStringString);
                    }
                    else if (state is IEnumerable<KeyValuePair<string, object>> keyValStringObject)
                    {
                        @event.SetTags(keyValStringObject.Select(k =>
                            new KeyValuePair<string, string>(k.Key, k.Value.ToString())));
                    }
                    else
                    {
                        // TODO: possible callback invocation here
                        @event.SetExtra("State of unknown type", state.GetType().ToString());
                    }
                }
            }

            @event.Breadcrumbs = @event.Breadcrumbs.AddRange(scope.Breadcrumbs);

            return @event;
        }

        public void Dispose()
        {
            // Worker should empty it's queue until SentryOptions.ShutdownTimeout
            (_worker as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}
