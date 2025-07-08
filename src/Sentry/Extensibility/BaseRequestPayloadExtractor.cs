using Sentry.Internal.Extensions;

namespace Sentry.Extensibility;

/// <summary>
/// Base type for payload extraction.
/// </summary>
public abstract class BaseRequestPayloadExtractor : IRequestPayloadExtractor
{
    /// <summary>
    /// Extract the payload of the <see cref="IHttpRequest"/>.
    /// </summary>
    public object? ExtractPayload(IHttpRequest request)
    {
        // Not to throw on code that ignores nullability warnings.
        if (request.IsNull())
        {
            return null;
        }

        if (request.Body is not { CanRead: true } || !IsSupported(request))
        {
            return null;
        }

        if (!request.Body.CanSeek)
        {
            // When RequestDecompression is enabled, the RequestDecompressionMiddleware will store a SizeLimitedStream
            // in the request body after decompression. Seek operations throw an exception, but we can still read the stream
            return DoExtractPayLoad(request);
        }

        var originalPosition = request.Body.Position;
        try
        {
            request.Body.Position = 0;
            return DoExtractPayLoad(request);
        }
        finally
        {
            request.Body.Position = originalPosition;
        }
    }

    /// <summary>
    /// Whether this implementation supports the <see cref="IHttpRequest"/>.
    /// </summary>
    protected abstract bool IsSupported(IHttpRequest request);

    /// <summary>
    /// The extraction that gets called in case <see cref="IsSupported"/> is true.
    /// </summary>
    protected abstract object? DoExtractPayLoad(IHttpRequest request);
}
