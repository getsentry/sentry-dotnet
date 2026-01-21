using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Sentry.AspNetCore.Grpc.Tests;

public class GrpcRequestHandler<TRequest, TResponse>
    where TRequest : class
    where TResponse : class
{
    public MethodDescriptor Method { get; set; }

    public Func<TRequest, ServerCallContext, Task<TResponse>> Handler
    {
        get => field ?? ((_, _) => Task.FromResult<TResponse>(null));
        set;
    }

    public TResponse Response { get; set; }
}
