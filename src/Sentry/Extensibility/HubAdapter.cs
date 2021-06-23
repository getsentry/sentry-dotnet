using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Sentry.Infrastructure;

namespace Sentry.Extensibility
{
    /// <summary>
    /// An implementation of <see cref="IHub" /> which forwards any call to <see cref="SentrySdk" />.
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
        public static readonly HubAdapter Instance = new();

        private HubAdapter() { }

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        public bool IsEnabled { [DebuggerStepThrough] get => SentrySdk.IsEnabled; }

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        public SentryId LastEventId { [DebuggerStepThrough] get => SentrySdk.LastEventId; }

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void ConfigureScope(Action<Scope> configureScope)
            => SentrySdk.ConfigureScope(configureScope);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
            => SentrySdk.ConfigureScopeAsync(configureScope);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public IDisposable PushScope()
            => SentrySdk.PushScope();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public IDisposable PushScope<TState>(TState state)
            => SentrySdk.PushScope(state);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void WithScope(Action<Scope> scopeCallback)
            => SentrySdk.WithScope(scopeCallback);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext)
            => SentrySdk.StartTransaction(context, customSamplingContext);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void BindException(Exception exception, ISpan span) =>
            SentrySdk.BindException(exception, span);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public ISpan? GetSpan()
            => SentrySdk.GetSpan();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public SentryTraceHeader? GetTraceHeader()
            => SentrySdk.GetTraceHeader();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void StartSession()
            => SentrySdk.StartSession();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void PauseSession()
            => SentrySdk.PauseSession();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void ResumeSession()
            => SentrySdk.ResumeSession();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
            => SentrySdk.EndSession();

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void BindClient(ISentryClient client)
            => SentrySdk.BindClient(client);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public void AddBreadcrumb(
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
            => SentrySdk.AddBreadcrumb(message, category, type, data, level);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AddBreadcrumb(
            ISystemClock clock,
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
            => SentrySdk.AddBreadcrumb(
                clock,
                message,
                category,
                type,
                data,
                level);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public SentryId CaptureEvent(SentryEvent evt)
            => SentrySdk.CaptureEvent(evt);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        public SentryId CaptureException(Exception exception)
            => SentrySdk.CaptureException(exception);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SentryId CaptureEvent(SentryEvent evt, Scope? scope)
            => SentrySdk.CaptureEvent(evt, scope);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureTransaction(Transaction transaction)
            => SentrySdk.CaptureTransaction(transaction);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>.
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureSession(SessionUpdate sessionUpdate)
            => SentrySdk.CaptureSession(sessionUpdate);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Task FlushAsync(TimeSpan timeout)
            => SentrySdk.FlushAsync(timeout);

        /// <summary>
        /// Forwards the call to <see cref="SentrySdk"/>
        /// </summary>
        [DebuggerStepThrough]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CaptureUserFeedback(UserFeedback sentryUserFeedback)
            => SentrySdk.CaptureUserFeedback(sentryUserFeedback);
    }
}
