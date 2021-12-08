using Google.Protobuf;

namespace Sentry.AspNetCore.Grpc;

internal class GrpcRequestAdapter<TRequest> : IProtobufRequest<TRequest>
    where TRequest : class, IMessage
{
    public GrpcRequestAdapter(TRequest request) => Request = request;

    public long? ContentLength => Request.CalculateSize();

    public TRequest Request { get; }
}
