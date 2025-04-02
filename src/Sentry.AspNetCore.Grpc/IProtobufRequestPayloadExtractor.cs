using Google.Protobuf;

namespace Sentry.AspNetCore.Grpc;

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
    public IMessage? ExtractPayload<TRequest>(IProtobufRequest<TRequest> request)
        where TRequest : class, IMessage;
}
