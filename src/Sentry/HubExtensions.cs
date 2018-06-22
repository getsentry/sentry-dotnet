using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
                data: data?.ToImmutableDictionary(),
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
                    clock: clock,
                    message: message,
                    type: type,
                    data: data?.ToImmutableDictionary(),
                    category: category,
                    level: level));
    }
}
