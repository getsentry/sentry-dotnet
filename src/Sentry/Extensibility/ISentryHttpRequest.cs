namespace Sentry.Extensibility;

/// <summary>
/// An abstraction to an HTTP Request.
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    /// The content length.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// The content type.
    /// </summary>
    public string? ContentType { get; }

    /// <summary>
    /// The request body.
    /// </summary>
    public Stream? Body { get; }

    /// <summary>
    /// Represents the parsed form values sent with the HttpRequest.
    /// </summary>
    public IEnumerable<KeyValuePair<string, IEnumerable<string>>>? Form { get; }
}
