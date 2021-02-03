#if NET5_0 || NETCOREAPP3_1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    public class SentryHttpMessageHandlerBuilderFilterTests
    {
        // Inserts a recorder into pipeline
        private class RecordingHandlerBuilderFilter : IHttpMessageHandlerBuilderFilter
        {
            private readonly RecordingHttpMessageHandler _handler;

            public RecordingHandlerBuilderFilter(RecordingHttpMessageHandler handler) => _handler = handler;

            public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) =>
                handlerBuilder =>
                {
                    handlerBuilder.AdditionalHandlers.Add(_handler);
                    next(handlerBuilder);
                };
        }

        [Fact]
        public async Task Generated_client_sends_Sentry_trace_header_automatically()
        {
            // Arrange

            // Will use this to record outgoing requests
            using var recorder = new RecordingHttpMessageHandler();

            var hub = new Internal.Hub(new SentryOptions
            {
                Dsn = DsnSamples.ValidDsnWithoutSecret
            });

            var server = new TestServer(new WebHostBuilder()
                .UseDefaultServiceProvider(di => di.ValidateScopes = true)
                .UseSentry()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHttpClient();

                    services.AddSingleton<IHttpMessageHandlerBuilderFilter>(new RecordingHandlerBuilderFilter(recorder));

                    services.RemoveAll(typeof(Func<IHub>));
                    services.AddSingleton<Func<IHub>>(() => hub);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseSentryTracing();

                    app.UseEndpoints(routes =>
                    {
                        routes.Map("/trigger", async ctx =>
                        {
                            using var httpClient = ctx.RequestServices
                                .GetRequiredService<IHttpClientFactory>()
                                .CreateClient();

                            await httpClient.GetAsync("https://example.com");
                        });
                    });
                })
            );

            var client = server.CreateClient();

            // Act
            await client.GetStringAsync("/trigger");

            var request = recorder.GetRequests().Single();

            // Assert
            request.Headers.Should().Contain(header => header.Key == "sentry-trace");
        }
    }
}
#endif
