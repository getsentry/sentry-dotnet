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
    /// An implementation of <see cref="IHub" /> which forwards any call to <see cref="SentryCore" />
    /// </summary>
    /// <remarks>
    /// Allows testing classes which otherwise would need to depend on static <see cref="SentryCore" />
    /// by having them depend on <see cref="IHub"/> instead, which can be mocked.
    /// </remarks>
    /// <inheritdoc cref="IHub" />
    [DebuggerStepThrough]
    public sealed class HubAdapter : IHub
    {
        /// <summary>
        /// The single instance which forwards all calls to <see cref="SentryCore"/>
        /// </summary>
        public static readonly HubAdapter Instance = new HubAdapter();

        private HubAdapter() { }

        public bool IsEnabled => SentryCore.IsEnabled;

        public void ConfigureScope(Action<Scope> configureScope)
            => SentryCore.ConfigureScope(configureScope);

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => SentryCore.ConfigureScopeAsync(configureScope);

        public IDisposable PushScope()
            => SentryCore.PushScope();

        public IDisposable PushScope<TState>(TState state)
            => SentryCore.PushScope(state);

        public void BindClient(ISentryClient client)
            => SentryCore.BindClient(client);

        public void AddBreadcrumb(
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => SentryCore.AddBreadcrumb(message, type, category, data, level);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddBreadcrumb(
            ISystemClock clock,
            string message,
            string type = null,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => SentryCore.AddBreadcrumb(
                clock: clock,
                message: message,
                type: type,
                data: data,
                category: category,
                level: level);

        public Guid CaptureEvent(SentryEvent evt)
            => SentryCore.CaptureEvent(evt);

        public Guid CaptureException(Exception exception)
            => SentryCore.CaptureException(exception);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public Guid CaptureEvent(SentryEvent evt, Scope scope)
            => SentryCore.CaptureEvent(evt, scope);
    }
}
