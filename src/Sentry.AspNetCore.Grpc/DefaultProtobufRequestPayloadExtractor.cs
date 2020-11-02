using Google.Protobuf;

namespace Sentry.AspNetCore.Grpc
{
    /// <summary>
    /// Default request payload extractor that will read the request as an <see cref="IMessage"/>
    /// </summary>
    public class DefaultProtobufRequestPayloadExtractor : IProtobufRequestPayloadExtractor
    {
        /// <summary>
        /// Extracts the request body of the <see cref="IProtobufRequest{TRequest}"/> as an <see cref="IMessage"/>.
        /// </summary>
        public IMessage ExtractPayload<TRequest>(IProtobufRequest<TRequest> request)
            where TRequest : class, IMessage
        {
            return request.Request;
        }
    }
}
