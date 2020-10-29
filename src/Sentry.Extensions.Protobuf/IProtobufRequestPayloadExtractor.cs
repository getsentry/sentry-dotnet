using Google.Protobuf;

namespace Sentry.Extensions.Protobuf
{
    /// <summary>
    /// A request extractor.
    /// </summary>
    public interface IProtobufRequestPayloadExtractor
    {
        /// <summary>
        /// Extracts the payload of the provided <see cref="IProtobufRequest{TRequest}"/>.
        /// </summary>
        /// <param name="request">The gRPC Request object.</param>
        /// <returns>The extracted payload.</returns>
        IMessage? ExtractPayload<TRequest>(IProtobufRequest<TRequest> request)
            where TRequest : class, IMessage;
    }
}
