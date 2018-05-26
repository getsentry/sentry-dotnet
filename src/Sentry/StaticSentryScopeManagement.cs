using System;
using System.Diagnostics;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// An adapter to SentryCore for testability
    /// </summary>
    /// <seealso cref="T:Sentry.ISentryScopeManagement" />
    /// <inheritdoc />
    public class StaticSentryScopeManagement : ISentryScopeManagement
    {
        /// <summary>
        /// The single instance which forwards all calls to <see cref="SentryCore"/>
        /// </summary>
        public static readonly StaticSentryScopeManagement Instance = new StaticSentryScopeManagement();

        private StaticSentryScopeManagement() { }

        /// <summary>
        /// Configures the scope through the callback.
        /// </summary>
        /// <param name="configureScope">The configure scope.</param>
        /// <inheritdoc />
        [DebuggerStepThrough]
        public void ConfigureScope(Action<Scope> configureScope)
            => SentryCore.ConfigureScope(configureScope);

        /// <summary>
        /// Creates a new scope that will terminate when disposed
        /// </summary>
        /// <returns>A disposable that when disposed, ends the created scope.</returns>
        /// <inheritdoc />
        [DebuggerStepThrough]
        public IDisposable PushScope() => SentryCore.PushScope();
    }
}
