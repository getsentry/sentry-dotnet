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
    /// An implementation of <see cref="ISentryClient" /> which forwards any call to <see cref="SentryCore" />
    /// </summary>
    /// <remarks>
    /// Allows testing classes which otherwise would need to depend on static <see cref="SentryCore" />
    /// by having them depend on ISdk instead, which can be mocked.
    /// </remarks>
    /// <inheritdoc cref="ISentryClient" />
    public sealed class SentryCoreAdapter : ISentryClient
    {
        /// <summary>
        /// The single instance which forwards all calls to <see cref="SentryCore"/>
        /// </summary>
        public static readonly SentryCoreAdapter Instance = new SentryCoreAdapter();

        private SentryCoreAdapter() { }

        public bool IsEnabled { [DebuggerStepThrough] get => SentryCore.IsEnabled; }

        [DebuggerStepThrough]
        public void ConfigureScope(Action<Scope> configureScope)
            => SentryCore.ConfigureScope(configureScope);

        [DebuggerStepThrough]
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => SentryCore.ConfigureScopeAsync(configureScope);

        [DebuggerStepThrough]
        public IDisposable PushScope()
            => SentryCore.PushScope();

        [DebuggerStepThrough]
        public IDisposable PushScope<TState>(TState state)
            => SentryCore.PushScope(state);

        [DebuggerStepThrough]
        public void AddBreadcrumb(
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => SentryCore.AddBreadcrumb(message, type, category, data, level);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        public Guid CaptureEvent(SentryEvent evt, Scope scope = null)
            => SentryCore.CaptureEvent(evt, scope);

        [DebuggerStepThrough]
        public Guid CaptureEvent(Func<SentryEvent> eventFactory)
            => SentryCore.CaptureEvent(eventFactory);

        [DebuggerStepThrough]
        public Guid CaptureException(Exception exception)
            => SentryCore.CaptureException(exception);
    }
}
