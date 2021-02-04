using System;
using System.Collections.Generic;
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
        public static readonly DisabledHub Instance = new();

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
        /// Returns a dummy transaction.
        /// </summary>
        public ITransaction StartTransaction(
            ITransactionContext context,
            IReadOnlyDictionary<string, object?> customSamplingContext) =>
            // Transactions from DisabledHub are always sampled out
            new Transaction(this, context) {IsSampled = false};

        /// <summary>
        /// Returns null.
        /// </summary>
        public ISpan? GetSpan() => null;

        /// <summary>
        /// Returns null.
        /// </summary>
        public SentryTraceHeader? GetTraceHeader() => null;

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
        public void CaptureTransaction(ITransaction transaction)
        {
        }

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
