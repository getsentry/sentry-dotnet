using System.Threading;
using System.Threading.Tasks;

namespace Sentry
{
    /// <summary>
    /// An abstraction to the transport of the event
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends the <see cref="SentryEvent" /> to Sentry asynchronously
        /// </summary>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="SentryResponse"/></returns>
        Task<SentryResponse> CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends the <see cref="SentryEvent" /> to Sentry
        /// </summary>
        /// <param name="event">The event to send to Sentry.</param>
        /// <returns><see cref="SentryResponse"/></returns>
        SentryResponse CaptureEvent(SentryEvent @event);
    }
}
