using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="IHub"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HubExtensions
    {
        /// <summary>
        /// Starts a transaction.
        /// </summary>
        public static ITransaction StartTransaction(this IHub hub, ITransactionContext context) =>
            hub.StartTransaction(context, new Dictionary<string, object?>());

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        public static ITransaction StartTransaction(
            this IHub hub,
            string name,
            string operation) =>
            hub.StartTransaction(new TransactionContext(name, operation));

        /// <summary>
        /// Starts a transaction.
        /// </summary>
        public static ITransaction StartTransaction(
            this IHub hub,
            string name,
            string operation,
            string description)
        {
            var transaction = hub.StartTransaction(name, operation);
            transaction.Description = description;

            return transaction;
        }

        /// <summary>
        /// Starts a transaction from the specified trace header.
        /// </summary>
        public static ITransaction StartTransaction(
            this IHub hub,
            string name,
            string operation,
            SentryTraceHeader traceHeader)
        {
            var context = new TransactionContext(
                // SpanId from the header becomes ParentSpanId on this transaction
                traceHeader.SpanId,
                traceHeader.TraceId,
                name,
                operation,
                traceHeader.IsSampled
            );

            return hub.StartTransaction(context);
        }

        /// <summary>
        /// Adds a breadcrumb to the current scope.
        /// </summary>
        /// <param name="hub">The Hub which holds the scope stack.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">Category.</param>
        /// <param name="type">Breadcrumb type.</param>
        /// <param name="data">Additional data.</param>
        /// <param name="level">Breadcrumb level.</param>
        public static void AddBreadcrumb(
            this IHub hub,
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (hub is null)
            {
                return;
            }

            hub.AddBreadcrumb(
                null,
                message,
                category,
                type,
                data != null ? new Dictionary<string, string>(data) : null,
                level);
        }

        /// <summary>
        /// Adds a breadcrumb using a custom <see cref="ISystemClock"/> which allows better testability.
        /// </summary>
        /// <param name="hub">The Hub which holds the scope stack.</param>
        /// <param name="clock">The system clock.</param>
        /// <param name="message">The message.</param>
        /// <param name="category">Category.</param>
        /// <param name="type">Breadcrumb type.</param>
        /// <param name="data">Additional data.</param>
        /// <param name="level">Breadcrumb level.</param>
        /// <remarks>
        /// This method is to be used by integrations to allow testing.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this IHub hub,
            ISystemClock? clock,
            string message,
            string? category = null,
            string? type = null,
            IDictionary<string, string>? data = null,
            BreadcrumbLevel level = default)
        {
            // Not to throw on code that ignores nullability warnings.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (hub is null)
            {
                return;
            }

            hub.ConfigureScope(
                s => s.AddBreadcrumb(
                    (clock ?? SystemClock.Clock).GetUtcNow(),
                    message,
                    category,
                    type,
                    data != null ? new Dictionary<string, string>(data) : null,
                    level));
        }

        /// <summary>
        /// Pushes a new scope while locking it which stop new scope creation.
        /// </summary>
        public static IDisposable PushAndLockScope(this IHub hub) => new LockedScope(hub);

        /// <summary>
        /// Lock the scope so subsequent <see cref="ISentryScopeManager.PushScope"/> don't create new scopes.
        /// </summary>
        /// <remarks>
        /// This is useful to stop following scope creation by other integrations
        /// like Loggers which guarantee log messages are not lost.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void LockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = true);

        /// <summary>
        /// Unlocks the current scope to allow subsequent calls to <see cref="ISentryScopeManager.PushScope"/> create new scopes.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void UnlockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = false);

        private sealed class LockedScope : IDisposable
        {
            private readonly IDisposable _scope;

            public LockedScope(IHub hub)
            {
                _scope = hub.PushScope();
                hub.LockScope();
            }

            public void Dispose() => _scope.Dispose();
        }
    }
}
