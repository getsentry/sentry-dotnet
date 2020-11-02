using Google.Protobuf;

namespace Sentry.AspNetCore.Grpc
{
    internal class GrpcRequestAdapter<TRequest> : IProtobufRequest<TRequest>
        where TRequest : class, IMessage
    {
        private readonly TRequest _request;

        public GrpcRequestAdapter(TRequest request) => _request = request;

        public long? ContentLength => _request.CalculateSize();

        public TRequest Request => _request;
    }
}
