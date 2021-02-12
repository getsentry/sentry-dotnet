using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public partial class IntegrationsTests : AspNetSentrySdkTestFixture
    {
        [Fact]
        public async Task UnhandledException_AvailableThroughLastExceptionFilter()
        {
            var expectedException = new Exception("test");
            var handler = new RequestHandler
            {
                Path = "/throw",
                Handler = _ => throw expectedException
            };

            Handlers = new[] { handler };
            Build();
            _ = await HttpClient.GetAsync(handler.Path);

            Assert.Same(expectedException, LastExceptionFilter.LastException);
        }

        [Fact]
        public async Task WebHost_SdkStartedViaStartupFilter_DisableSdkOnShutdown()
        {
            var logger = Substitute.For<ILogger>();
            var factory = Substitute.For<ILoggerFactory>();
            _ = factory.CreateLogger(Arg.Any<string>()).Returns(logger);

            AfterConfigureBuilder = builder =>
            {
                // This is what Serilog does and then ignores adding a LoggerProvider
                // essentially replacing the standard MEL backend with its own.
                // When logging integrations behave like this, the SDK should work normally
                // simply without the MEL integration to event/breadcrumb
                // Note that it runs After UseSentry
                _ = builder.ConfigureServices(collection =>
                        collection.AddSingleton(factory));
            };

            Build();

            // Make sure custom factory was used instead of default one
            _ = factory.Received().CreateLogger(Arg.Any<string>());

            Assert.True(SentrySdk.IsEnabled);

            await TestServer.Host.StopAsync();
            Assert.False(SentrySdk.IsEnabled);
        }
    }
}
