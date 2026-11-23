using Sentry.Android;
using Sentry.Internal.Http;
using Xamarin.Android.Net;

namespace Sentry.Tests.Platforms.Android;

public class DefaultSentryHttpClientFactoryAndroidTests
{
    private const string SaasDsn = "https://key@o1.ingest.sentry.io/1";

    private readonly SentryOptions _options = new() { Dsn = SaasDsn };

    [Fact]
    public void Create_Api25SaasDsn_UsesSentryRootCaMessageHandler()
    {
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 25 };

        using var client = sut.Create(_options);

        Assert.Contains(client.GetMessageHandlers(), h => h is SentryRootCaMessageHandler);
    }

    [Fact]
    public void Create_Api26SaasDsn_DoesNotUseSentryRootCaMessageHandler()
    {
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 26 };

        using var client = sut.Create(_options);

        Assert.DoesNotContain(client.GetMessageHandlers(), h => h is SentryRootCaMessageHandler);
    }

    [Fact]
    public void Create_Api25OtherHost_DoesNotUseSentryRootCaMessageHandler()
    {
        _options.Dsn = ValidDsn;
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 25 };

        using var client = sut.Create(_options);

        Assert.DoesNotContain(client.GetMessageHandlers(), h => h is SentryRootCaMessageHandler);
    }

    [Fact]
    public void Create_Api25UserHandler_UsesUserHandler()
    {
        var userHandler = new AndroidMessageHandler();
        _options.CreateHttpMessageHandler = () => userHandler;
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 25 };

        using var client = sut.Create(_options);

        Assert.Contains(client.GetMessageHandlers(), h => ReferenceEquals(userHandler, h));
        Assert.DoesNotContain(client.GetMessageHandlers(), h => h is SentryRootCaMessageHandler);
    }

    [Fact]
    public void Create_Api25UserHandlerReturnsNull_UsesSentryRootCaMessageHandler()
    {
        _options.CreateHttpMessageHandler = () => null;
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 25 };

        using var client = sut.Create(_options);

        Assert.Contains(client.GetMessageHandlers(), h => h is SentryRootCaMessageHandler);
    }

    [Fact]
    public void Create_Api25ProxyAndDecompression_AppliedToSentryRootCaMessageHandler()
    {
        var proxy = new WebProxy("http://localhost:8080");
        _options.HttpProxy = proxy;
        _options.DecompressionMethods = DecompressionMethods.GZip;
        var sut = new DefaultSentryHttpClientFactory { AndroidSdkInt = 25 };

        using var client = sut.Create(_options);

        var handler = client.GetMessageHandlers().OfType<SentryRootCaMessageHandler>().Single();
        Assert.Same(proxy, handler.Proxy);
        Assert.Equal(DecompressionMethods.GZip, handler.AutomaticDecompression);
    }
}
