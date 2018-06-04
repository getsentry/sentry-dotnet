using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Extension methods for <see cref="ISentryClient"/>
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class SentryClientExtensions
    {
        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="client">The Sentry client.</param>
        /// <param name="ex">The exception.</param>
        /// <returns></returns>
        public static SentryResponse CaptureException(this ISentryClient client, Exception ex)
            => client.CaptureEvent(new SentryEvent(ex));

        public static void AddBreadcrumb(
            this ISentryClient client,
            string message,
            string type,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => client.AddBreadcrumb(
                clock: null,
                message: message,
                type: type,
                data: data?.ToImmutableDictionary(),
                category: category,
                level: level);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddBreadcrumb(
            this ISentryClient client,
            ISystemClock clock,
            string message,
            string type = null,
            string category = null,
            IDictionary<string, string> data = null,
            BreadcrumbLevel level = default)
            => client.ConfigureScope(
                s => s.AddBreadcrumb(new Breadcrumb(
                    timestamp: (clock ?? SystemClock.Clock).GetUtcNow(),
                    message: message,
                    type: type,
                    data: data?.ToImmutableDictionary(),
                    category: category,
                    level: level)));
    }
}
