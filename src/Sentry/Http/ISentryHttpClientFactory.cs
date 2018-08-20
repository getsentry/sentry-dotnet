using System.Net.Http;

namespace Sentry.Http
{
    /// <summary>
    /// Sentry <see cref="HttpClient"/> factory
    /// </summary>
    public interface ISentryHttpClientFactory
    {
        /// <summary>
        /// Creates an HttpClient using the specified options.
        /// </summary>
        /// <param name="dsn">The DSN.</param>
        /// <param name="options">The options.</param>
        /// <returns><see cref="HttpClient"/></returns>
        HttpClient Create(Dsn dsn, SentryOptions options);
    }
}
