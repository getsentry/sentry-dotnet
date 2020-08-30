using System;
using System.Threading.Tasks;
using Sentry.Protocol;

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
        public static DisabledHub Instance = new DisabledHub();

        /// <summary>
        /// Always disabled.
        /// </summary>
        public bool IsEnabled => false;

        private DisabledHub() { }

        /// <summary>
        /// No-Op.
        /// </summary>
        /// <param name="configureScope"></param>
        public void ConfigureScope(Action<Scope> configureScope) { }
        /// <summary>
        /// No-Op.
        /// </summary>
        /// <param name="configureScope"></param>
        /// <returns></returns>
        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

        /// <summary>
        /// No-Op.
        /// </summary>
        /// <returns></returns>
        public IDisposable PushScope() => this;
        /// <summary>
        /// No-Op.
        /// </summary>
        /// <param name="state"></param>
        /// <typeparam name="TState"></typeparam>
        /// <returns></returns>
        public IDisposable PushScope<TState>(TState state) => this;

        /// <summary>
        /// No-Op.
        /// </summary>
        public void WithScope(Action<Scope> scopeCallback) { }

        /// <summary>
        /// No-Op.
        /// </summary>
        public void BindClient(ISentryClient client) { }

        /// <summary>
        /// No-Op.
        /// </summary>
        public SentryId CaptureEvent(SentryEvent evt, Scope scope = null) => SentryId.Empty;

        /// <summary>
        /// No-Op.
        /// </summary>
        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        /// <summary>
        /// No-Op.
        /// </summary>
        public void Dispose() { }

        public IHub Clone() => Instance;

        /// <summary>
        /// No-Op.
        /// </summary>
        public SentryId LastEventId => SentryId.Empty;
    }
}
