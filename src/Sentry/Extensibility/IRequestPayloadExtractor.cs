namespace Sentry.Extensibility;

/// <summary>
/// A request body extractor.
/// </summary>
public interface IRequestPayloadExtractor
{
    /// <summary>
    /// Extracts the payload of the provided <see cref="IHttpRequest"/>.
    /// </summary>
    /// <param name="request">The HTTP Request object.</param>
    /// <returns>The extracted payload.</returns>
    public object? ExtractPayload(IHttpRequest request);
}
