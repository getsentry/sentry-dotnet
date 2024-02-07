namespace Sentry.Http;

/// <summary>
/// Sentry <see cref="HttpClient"/> factory.
/// </summary>
public interface ISentryHttpClientFactory
{
    /// <summary>
    /// Creates an HttpClient using the specified options.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <returns><see cref="HttpClient"/>.</returns>
    HttpClient Create(SentryOptions options);
}
