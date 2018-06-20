using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    // Tests that depend on static SentryCore have to be on the same collection (avoid running in parallel)
    public class SentryCoreDependentCollection
    {

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
