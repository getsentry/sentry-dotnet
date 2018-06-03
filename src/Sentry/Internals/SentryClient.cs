using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Extensibility.Http;
using Sentry.Protocol;

namespace Sentry.Internals
{
    internal class SentryClient : ISentryClient, IDisposable
    {
        private readonly SentryOptions _options;
        private readonly ITransport _transport;

        // Testability
        internal IInternalScopeManagement ScopeManagement { get; }

        public SentryClient(SentryOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            // TODO: Subscribing or not should be based on the Options
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Create proper client based on Options
            //_transport = new SentryClient(options);
            _transport = new HttpTransport();
            ScopeManagement = new SentryScopeManagement(options);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // TODO: avoid stackoverflow
            if (e.ExceptionObject is Exception ex)
            {
                // TODO: Add to Scope: Exception Mechanism = e.IsTerminating
                this.CaptureException(ex);
            }
        }

        public bool IsEnabled => true;

        public void ConfigureScope(Action<Scope> configureScope) => ScopeManagement.ConfigureScope(configureScope);
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => ScopeManagement.ConfigureScopeAsync(configureScope);

        public IDisposable PushScope() => ScopeManagement.PushScope();

        public IDisposable PushScope<TState>(TState state) => ScopeManagement.PushScope(state);

        public async Task<SentryResponse> CaptureEventAsync(Func<Task<SentryEvent>> eventFactory)
        {
            // SDK enabled, invoke the factory and the client, asynchronously
            var @event = await eventFactory().ConfigureAwait(false);
            return await CaptureEventAsync(@event).ConfigureAwait(false);
        }

        public SentryResponse CaptureEvent(Func<SentryEvent> eventFactory)
        {
            var @event = eventFactory();
            return CaptureEvent(@event);
        }

        public Task<SentryResponse> CaptureEventAsync(SentryEvent @event)
        {
            @event = PrepareEvent(@event);
            return _transport.CaptureEventAsync(@event);
        }

        public SentryResponse CaptureEvent(SentryEvent @event)
        {
            @event = PrepareEvent(@event);
            return _transport.CaptureEvent(@event);
        }

        private SentryEvent PrepareEvent(SentryEvent @event)
        {
            var scope = ScopeManagement.GetCurrent();
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

            // Client should empty it's queue until SentryOptions.ShutdownTimeout
            (_transport as IDisposable)?.Dispose();

            // TODO: set _isDisposed and throw ObjectDisposed from members
        }
    }
}
