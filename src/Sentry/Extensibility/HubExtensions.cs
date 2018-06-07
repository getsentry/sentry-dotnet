using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry.Extensibility
{
    /// <summary>
    /// Extension methods for <see cref="IHub"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class HubExtensions
    {
        public static void AddBreadcrumb(
            this IHub hub,
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => hub.AddBreadcrumb(
                clock: null,
                message: message,
                type: type,
                data: data?.ToImmutableDictionary(),
                category: category,
                level: level);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this IHub hub,
            ISystemClock clock,
            string message,
            string type = null,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => hub.ConfigureScope(
                s => s.AddBreadcrumb(new Breadcrumb(
                    timestamp: (clock ?? SystemClock.Clock).GetUtcNow(),
                    message: message,
                    type: type,
                    data: data?.ToImmutableDictionary(),
                    category: category,
                    level: level)));
    }
}
