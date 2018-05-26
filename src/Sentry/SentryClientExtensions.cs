using System;
using System.ComponentModel;
using System.Threading.Tasks;
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
        /// <param name="scope">Scope</param>
        /// <returns></returns>
        public static Task<SentryResponse> CaptureExceptionAsync(this ISentryClient client, Exception ex, Scope scope)
        {
            return client.CaptureEventAsync(new SentryEvent(ex), scope);
        }

        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="client">The Sentry client.</param>
        /// <param name="ex">The exception.</param>
        /// <param name="scope">Scope</param>
        /// <returns></returns>
        public static SentryResponse CaptureException(this ISentryClient client, Exception ex, Scope scope)
        {
            return client.CaptureEvent(new SentryEvent(ex), scope);
        }
    }
}
