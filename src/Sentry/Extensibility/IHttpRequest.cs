namespace Sentry.Extensibility;

/// <summary>
/// An abstraction to an HTTP Request.
/// </summary>
public interface IHttpRequest
{
    /// <summary>
    /// The content length.
    /// </summary>
    long? ContentLength { get; }

    /// <summary>
    /// The content type.
    /// </summary>
    string? ContentType { get; }

    /// <summary>
    /// The request body.
    /// </summary>
    Stream? Body { get; }

    /// <summary>
    /// Represents the parsed form values sent with the HttpRequest.
    /// </summary>
    IEnumerable<KeyValuePair<string, IEnumerable<string>>>? Form { get; }
}
