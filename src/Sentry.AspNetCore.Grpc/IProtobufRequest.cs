namespace Sentry.AspNetCore.Grpc;

/// <summary>
/// An abstraction to a gRPC Request.
/// </summary>
public interface IProtobufRequest<TRequest>
{
    /// <summary>
    /// The content length.
    /// </summary>
    public long? ContentLength { get; }

    /// <summary>
    /// The request message.
    /// </summary>
    public TRequest Request { get; }
}
