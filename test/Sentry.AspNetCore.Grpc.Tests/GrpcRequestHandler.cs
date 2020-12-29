using System;
using System.Threading.Tasks;
using Google.Protobuf.Reflection;
using Grpc.Core;

namespace Sentry.AspNetCore.Grpc.Tests
{
    public class GrpcRequestHandler<TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        public MethodDescriptor Method { get; set; }

        private Func<TRequest, ServerCallContext, Task<TResponse>> _handler;

        public Func<TRequest, ServerCallContext, Task<TResponse>> Handler
        {
            get => _handler ?? ((_, _) => Task.FromResult<TResponse>(null));
            set => _handler = value;
        }

        public TResponse Response { get; set; }
    }
}
