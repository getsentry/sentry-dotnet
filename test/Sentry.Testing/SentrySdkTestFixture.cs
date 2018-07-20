using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Testing
{
    public class SentrySdkTestFixture : IDisposable
    {
        private TestServer _testServer;

        public HttpClient HttpClient { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public Action<IWebHostBuilder> ConfigureBuilder { get; set; }
        public Action<IServiceCollection> ConfigureServices { get; set; }

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

        protected virtual void Build()
        {
            var builder = new WebHostBuilder();
            builder.ConfigureServices(s =>
            {
                var lastException = new LastExceptionFilter();
                s.AddSingleton<IStartupFilter>(lastException);
                s.AddSingleton(lastException);

                ConfigureServices?.Invoke(s);
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
            SentrySdk.Close();
        }
    }
}
