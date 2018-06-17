using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    // Tests that depend on static SentryCore have to be on the same collection (avoid running in parallel)
    public class SentryCoreDependentCollection : IDisposable
    {
        private TestServer _testServer;

        public HttpClient HttpClient { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public Action<IWebHostBuilder> ConfigureBuilder { get; set; }

        public LastExceptionFilter LastExceptionFilter { get; private set; }

        public IReadOnlyCollection<RequestHandler> Handlers { get; set; } = new[]
        {
            new RequestHandler
            {
                Path = "/",
                Response = "home"
            },
            new RequestHandler
            {
                Path = "/throw",
                Handler = _ => throw new Exception("test error")
            }
        };

        public SentryCoreDependentCollection()
        {
            SentryCore.Close(); // In case SDK was not closed by previous test.
        }

        public void Dispose()
        {
            HttpClient?.Dispose();
            _testServer?.Dispose();
            SentryCore.Close();
        }

        public void Build()
        {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(s =>
            {
                var lastException = new LastExceptionFilter();
                s.AddSingleton<IStartupFilter>(lastException);
                s.AddSingleton(lastException);
            });
            var sentry = FakeSentryServer.CreateServer();
            var sentryHttpClient = sentry.CreateClient();
            ConfigureBuilder = b => b.UseSentry(options =>
            {
                options.Dsn = DsnSamples.ValidDsnWithSecret;
                options.Init(i =>
                {
                    i.Http(h =>
                    {
                        h.SentryHttpClientFactory = new DelegateHttpClientFactory((d, o)
                            => sentryHttpClient);
                    });
                });
            });
            builder.Configure(app =>
            {
                app.Use(async (context, next) =>
                {
                    var handler = Handlers.FirstOrDefault(p => p.Path == context.Request.Path);

                    await (handler?.Handler(context) ?? next());
                });
            });

            ConfigureBuilder?.Invoke(builder);

            _testServer = new TestServer(builder);
            HttpClient = _testServer.CreateClient();
            ServiceProvider = _testServer.Host.Services;
            LastExceptionFilter = ServiceProvider.GetRequiredService<LastExceptionFilter>();
        }
    }

    [CollectionDefinition(nameof(SentryCoreDependentCollection))]
    public sealed class TestServerCollection : ICollectionFixture<SentryCoreDependentCollection>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
        // See: http://xunit.github.io/docs/shared-context.html#collection-fixture
    }
}
