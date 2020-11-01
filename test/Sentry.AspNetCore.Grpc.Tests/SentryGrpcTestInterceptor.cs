using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Grpc.Tests
{
    public class SentryGrpcTestInterceptor : Interceptor
    {
        public static Exception LastException { get; set; }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var httpContext = context.GetHttpContext();

            var handlers =
                httpContext.RequestServices.GetService<IEnumerable<GrpcRequestHandler<TRequest, TResponse>>>();

            var methodName = context.Method.Substring(1).Replace('/', '.');

            var handler = handlers.FirstOrDefault(p => p.Method.FullName == methodName);

            try
            {
                if (handler == null)
                {
                    return await continuation(request, context);
                }

                return await handler.Handler(request, context);
            }
            catch (Exception e)
            {
                LastException = e;

                throw;
            }
        }
    }
}
