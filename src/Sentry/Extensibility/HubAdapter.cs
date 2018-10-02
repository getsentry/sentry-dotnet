using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    /// <summary>
    /// An implementation of <see cref="IHub" /> which forwards any call to <see cref="SentrySdk" />
    /// </summary>
    /// <remarks>
    /// Allows testing classes which otherwise would need to depend on static <see cref="SentrySdk" />
    /// by having them depend on <see cref="IHub"/> instead, which can be mocked.
    /// </remarks>
    /// <inheritdoc cref="IHub" />
    [DebuggerStepThrough]
    public sealed class HubAdapter : IHub
    {
        /// <summary>
        /// The single instance which forwards all calls to <see cref="SentrySdk"/>
        /// </summary>
        public static readonly HubAdapter Instance = new HubAdapter();

        private HubAdapter() { }

        public bool IsEnabled { [DebuggerStepThrough] get => SentrySdk.IsEnabled; }

        public Guid LastEventId { [DebuggerStepThrough] get => SentrySdk.LastEventId; }

        [DebuggerStepThrough]
        public void ConfigureScope(Action<Scope> configureScope)
            => SentrySdk.ConfigureScope(configureScope);

        [DebuggerStepThrough]
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => SentrySdk.ConfigureScopeAsync(configureScope);

        [DebuggerStepThrough]
        public IDisposable PushScope()
            => SentrySdk.PushScope();

        [DebuggerStepThrough]
        public IDisposable PushScope<TState>(TState state)
            => SentrySdk.PushScope(state);

        [DebuggerStepThrough]
        public void WithScope(Action<Scope> scopeCallback)
            => SentrySdk.WithScope(scopeCallback);

        [DebuggerStepThrough]
        public void BindClient(ISentryClient client)
            => SentrySdk.BindClient(client);

        [DebuggerStepThrough]
        public void AddBreadcrumb(
            string message,
            string category = null,
            string type = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => SentrySdk.AddBreadcrumb(message, category, type, data, level);

        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddBreadcrumb(
            ISystemClock clock,
            string message,
            string category = null,
            string type = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => SentrySdk.AddBreadcrumb(
                clock: clock,
                message: message,
                type: type,
                data: data,
                category: category,
                level: level);

        [DebuggerStepThrough]
        public Guid CaptureEvent(SentryEvent evt)
            => SentrySdk.CaptureEvent(evt);

        [DebuggerStepThrough]
        public Guid CaptureException(Exception exception)
            => SentrySdk.CaptureException(exception);

        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Guid CaptureEvent(SentryEvent evt, Scope scope)
            => SentrySdk.CaptureEvent(evt, scope);
    }
}
