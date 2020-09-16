using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Extensibility
{
    /// <summary>
    /// An abstraction to the transport of the event.
    /// </summary>
    public interface ITransport
    {
        /// <summary>
        /// Sends the <see cref="SentryEvent" /> to Sentry asynchronously.
        /// </summary>
        /// <param name="event">The event to send to Sentry.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        Task CaptureEventAsync(SentryEvent @event, CancellationToken cancellationToken = default);
    }
}
