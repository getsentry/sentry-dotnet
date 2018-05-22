using System;
using System.ComponentModel;
using System.Threading.Tasks;

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
        public static Task<SentryResponse> CaptureExceptionAsync(this ISentryClient client, Exception ex)
        {
            return client.CaptureEventAsync(new SentryEvent(ex));
        }

        /// <summary>
        /// Captures the exception.
        /// </summary>
        /// <param name="client">The Sentry client.</param>
        /// <param name="ex">The exception.</param>
        /// <returns></returns>
        public static SentryResponse CaptureException(this ISentryClient client, Exception ex)
        {
            return client.CaptureEvent(new SentryEvent(ex));
        }
    }
}
