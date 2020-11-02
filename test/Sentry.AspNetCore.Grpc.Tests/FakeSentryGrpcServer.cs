using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.AspNetCore.Grpc.Tests
{
    public static class FakeSentryGrpcServer
    {
        public static TestServer CreateServer<TService, TRequest, TResponse>
            (IReadOnlyCollection<GrpcRequestHandler<TRequest, TResponse>> handlers)
            where TService : class
            where TRequest : class
            where TResponse : class
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddGrpc(options =>
                    {
                        options.Interceptors.Add<SentryGrpcTestInterceptor>();
                    });

                    foreach (var handler in handlers)
                    {
                        services.AddSingleton(handler);
                    }
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGrpcService<TService>();
                    });
                });

            return new TestServer(builder);
        }
    }
}
