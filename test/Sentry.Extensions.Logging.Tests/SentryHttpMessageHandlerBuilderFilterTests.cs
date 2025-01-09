using Microsoft.Extensions.Http;

namespace Sentry.Extensions.Logging.Tests;

public class SentryHttpMessageHandlerBuilderFilterTests
{
    [SkippableFact]
    public void Configure_HandlerEnabled_ShouldAddSentryHttpMessageHandler()
    {
#if __ANDROID__
        Skip.If(true, "Can't create proxies for classes without parameterless constructors on Android");
#endif

        // Arrange
        var hub = Substitute.For<IHub>();
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions { DisableSentryHttpMessageHandler = false };

        var filter = new SentryHttpMessageHandlerBuilderFilter(() => hub);
        var handlerBuilder = Substitute.For<HttpMessageHandlerBuilder>();
        handlerBuilder.AdditionalHandlers.Returns(new List<DelegatingHandler>());
        Action<HttpMessageHandlerBuilder> next = _ => { };

        // Act
        var configure = filter.Configure(next);
        configure(handlerBuilder);

        // Assert
        handlerBuilder.AdditionalHandlers.Should().ContainSingle(h => h is SentryHttpMessageHandler);
    }

    [SkippableFact]
    public void Configure_HandlerDisabled_ShouldNotAddSentryHttpMessageHandler()
    {
#if __ANDROID__
        Skip.If(true, "Can't create proxies for classes without parameterless constructors on Android");
#endif

        // Arrange
        var hub = Substitute.For<IHub>();
        SentryClientExtensions.SentryOptionsForTestingOnly = new SentryOptions { DisableSentryHttpMessageHandler = true };

        var filter = new SentryHttpMessageHandlerBuilderFilter(() => hub);
        var handlerBuilder = Substitute.For<HttpMessageHandlerBuilder>();
        handlerBuilder.AdditionalHandlers.Returns(new List<DelegatingHandler>());
        Action<HttpMessageHandlerBuilder> next = _ => { };

        // Act
        var configure = filter.Configure(next);
        configure(handlerBuilder);

        // Assert
        handlerBuilder.AdditionalHandlers.Should().NotContain(h => h is SentryHttpMessageHandler);
    }
}
