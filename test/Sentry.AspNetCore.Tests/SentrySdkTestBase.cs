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
    [Collection(nameof(SentryCoreDependentCollection))]
    public abstract class SentrySdkTestBase : IDisposable
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

        public void Dispose()
        {
            SentryCore.Close();
        }
    }
}
