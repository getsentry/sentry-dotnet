namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// An abstraction to a gRPC Request.
    /// </summary>
    public interface IProtobufRequest<TRequest>
    {
        /// <summary>
        /// The content length.
        /// </summary>
        long? ContentLength { get; }

        /// <summary>
        /// The request message.
        /// </summary>
        TRequest Request { get; }
    }
}
