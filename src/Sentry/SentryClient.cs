using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sentry.Extensibility;
using Sentry.Extensibility.Http;
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
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            // TODO: Subscribing or not should be based on the Options
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            _worker = new BackgroundWorker(
                new HttpTransport(),
                options.BackgroundWorkerOptions);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: avoid stack overflow
            if (e.ExceptionObject is Exception ex)
            {
                // TODO: Add to Scope: Exception Mechanism = e.IsTerminating
                this.CaptureException(ex);
            }
        }

        public Guid CaptureEvent(SentryEvent @event, Scope scope = null)
        {
            // TODO: Apply scope to event
            var id = _failureId;
            try
            {
                @event = PrepareEvent(@event, scope);
                if (_worker.EnqueueEvent(@event))
                {
                    id = @event.EventId;
                }
                else
                {
                    // TODO: Notify error handler
                    Trace.WriteLine("Failed to enqueue event. Current queue depth: " + _worker.QueuedItems);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString()); // TODO: logger
            }

            return id;
        }

        private SentryEvent PrepareEvent(SentryEvent @event, Scope scope)
        {
            // TODO: Consider multiple events being sent with the same scope:
            // Wherever this code will end up, it should evaluate only once
            if (scope.States != null)
            {
                foreach (var state in scope.States)
                {
                    if (state is string scopeString)
                    {
                        @event.SetTag("scope", scopeString);
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

            @event = _options.BeforeSend?.Invoke(@event);

            return @event;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;

            // Worker should empty it's queue until SentryOptions.ShutdownTimeout
            (_worker as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}
