using System;
using System.Collections.Generic;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Sentry.Testing;

namespace Sentry.AspNetCore.Grpc.Tests
{
    // Allows integration tests the include the background worker and mock only the gRPC bits
    public class SentryGrpcSdkTestFixture : SentrySdkTestFixture
    {
        protected Action<SentryAspNetCoreOptions> Configure;

        protected Action<WebHostBuilder> AfterConfigureBuilder;

        public GrpcChannel Channel { get; set; }

        public IReadOnlyCollection<GrpcRequestHandler<TestRequest, TestResponse>> GrpcHandlers { get; set; } = new[]
        {
            new GrpcRequestHandler<TestRequest, TestResponse>
            {
                Method = TestService.Descriptor.FindMethodByName("Test"), Response = new TestResponse()
            },
            new GrpcRequestHandler<TestRequest, TestResponse>
            {
                Method = TestService.Descriptor.FindMethodByName("TestThrow"),
                Handler = (_, _) => throw new Exception("test error")
            }
        };

        protected override void ConfigureBuilder(WebHostBuilder builder)
        {
            var sentry = FakeSentryGrpcServer.CreateServer<GrpcTestService, TestRequest, TestResponse>(GrpcHandlers);
            var sentryHttpClient = sentry.CreateClient();
            _ = builder.UseSentry(sentryBuilder =>
            {
                sentryBuilder.AddGrpc();
                sentryBuilder.AddSentryOptions(options =>
                {
                    options.Dsn = DsnSamples.ValidDsnWithSecret;
                    options.SentryHttpClientFactory = new DelegateHttpClientFactory(_ => sentryHttpClient);

                    Configure?.Invoke(options);
                });
            });

            Channel = GrpcChannel.ForAddress("http://test-server",
                new GrpcChannelOptions { HttpClient = sentryHttpClient });

            AfterConfigureBuilder?.Invoke(builder);
        }
    }
}
