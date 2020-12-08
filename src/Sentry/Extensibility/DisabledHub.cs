using System;
using System.Threading.Tasks;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Disabled Hub.
    /// </summary>
    public class DisabledHub : IHub, IDisposable
    {
        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static readonly DisabledHub Instance = new DisabledHub();

        /// <summary>
        /// Always disabled.
        /// </summary>
        public bool IsEnabled => false;

        private DisabledHub()
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public void ConfigureScope(Action<Scope> configureScope)
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        /// <summary>
        /// No-Op.
        /// </summary>
        public IDisposable PushScope() => this;

        /// <summary>
        /// No-Op.
        /// </summary>
        public IDisposable PushScope<TState>(TState state) => this;

        /// <summary>
        /// No-Op.
        /// </summary>
        public void WithScope(Action<Scope> scopeCallback)
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public void BindClient(ISentryClient client)
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null) => SentryId.Empty;

        /// <summary>
        /// No-Op.
        /// </summary>
        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        /// <summary>
        /// No-Op.
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
        }

        /// <summary>
        /// No-Op.
        /// </summary>
        public SentryId LastEventId => SentryId.Empty;
    }
}
