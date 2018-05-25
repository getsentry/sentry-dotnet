using System.Threading;
using System.Threading.Tasks;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// Sentry client
    /// </summary>
    public interface ISentryClient
    {
        /// <summary>
        /// Sends the <see cref="SentryEvent" /> to Sentry asynchronously
        /// </summary>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="scope">The scope to attach to the event.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="SentryResponse"/></returns>
        Task<SentryResponse> CaptureEventAsync(SentryEvent @event, Scope scope, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the <see cref="SentryEvent" /> to Sentry
        /// </summary>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="scope">The scope to attach to the event.</param>
        /// <returns><see cref="SentryResponse"/></returns>
        SentryResponse CaptureEvent(SentryEvent @event, Scope scope);
    }
}
