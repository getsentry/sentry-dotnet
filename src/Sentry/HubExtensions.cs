using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="IHub"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HubExtensions
    {
        /// <summary>
        /// Adds a breadcrumb to the current scope
        /// </summary>
        /// <param name="hub">The Hub which holds the scope stack</param>
        /// <param name="message">The message</param>
        /// <param name="category">Category</param>
        /// <param name="type">Breadcrumb type</param>
        /// <param name="data">Additional data</param>
        /// <param name="level">Breadcrumb level</param>
        public static void AddBreadcrumb(
            this IHub hub,
            string message,
            string category = null,
            string type = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => hub.AddBreadcrumb(
                clock: null,
                message: message,
                type: type,
                data: data != null ? new Dictionary<string,string>(data) : null,
                category: category,
                level: level);

        /// <summary>
        /// Adds a breadcrumb using a custom <see cref="ISystemClock"/> which allows better testability
        /// </summary>
        /// <param name="hub">The Hub which holds the scope stack</param>
        /// <param name="clock">The system clock</param>
        /// <param name="message">The message</param>
        /// <param name="category">Category</param>
        /// <param name="type">Breadcrumb type</param>
        /// <param name="data">Additional data</param>
        /// <param name="level">Breadcrumb level</param>
        /// <remarks>
        /// This method is to be used by integrations to allow testing
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this IHub hub,
            ISystemClock clock,
            string message,
            string category = null,
            string type = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => hub.ConfigureScope(
                s => s.AddBreadcrumb(
                    timestamp: (clock ?? SystemClock.Clock).GetUtcNow(),
                    message: message,
                    category: category,
                    type: type,
                    data: data != null ? new Dictionary<string, string>(data) : null,
                    level: level));

        /// <summary>
        /// Pushes a new scope while locking it which stop new scope creation
        /// </summary>
        /// <param name="hub"></param>
        /// <returns></returns>
        public static IDisposable PushAndLockScope(this IHub hub) => new LockedScope(hub);

        /// <summary>
        /// Lock the scope so subsequent <see cref="ISentryScopeManager.PushScope"/> don't create new scopes.
        /// </summary>
        /// <remarks>
        /// This is useful to stop following scope creation by other integrations
        /// like Loggers which guarantee log messages are not lost
        /// </remarks>
        /// <param name="hub"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void LockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = true);

        /// <summary>
        /// Unlocks the current scope to allow subsequent calls to <see cref="ISentryScopeManager.PushScope"/> create new scopes.
        /// </summary>
        /// <param name="hub"></param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void UnlockScope(this IHub hub) => hub.ConfigureScope(c => c.Locked = false);

        private sealed class LockedScope : IDisposable
        {
            private readonly IDisposable _scope;

            public LockedScope(IHub hub)
            {
                Debug.Assert(hub != null);

                _scope = hub.PushScope();
                hub.LockScope();
            }

            public void Dispose() => _scope.Dispose();
        }
    }
}
