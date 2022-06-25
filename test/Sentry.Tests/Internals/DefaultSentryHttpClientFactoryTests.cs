using System.IO.Compression;
using System.Net.Http;
using Sentry.Internal.Http;

namespace Sentry.Tests.Internals;

public class DefaultSentryHttpClientFactoryTests
{
    [Fact]
    public void Create_Returns_HttpClient()
    {
        var sut = new DefaultSentryHttpClientFactory();

        Assert.NotNull(sut.Create(ValidDsnOptions.Instance));
    }

    [Fact]
    public void Create_CompressionLevelNoCompression_NoGzipRequestBodyHandler()
    {
        var options = new ValidDsnOptions
        {
            RequestBodyCompressionLevel = CompressionLevel.NoCompression
        };

        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        foreach (var handler in client.GetMessageHandlers())
        {
            Assert.IsNotType<GzipRequestBodyHandler>(handler);
        }
    }

    [Theory]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.Optimal)]
    public void Create_CompressionLevelEnabled_ByDefault_IncludesGzipRequestBodyHandler(CompressionLevel level)
    {
        var options = new ValidDsnOptions
        {
            RequestBodyCompressionLevel = level
        };

        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        Assert.Contains(client.GetMessageHandlers(), h => h.GetType() == typeof(GzipBufferedRequestBodyHandler));
    }

    [Theory]
    [InlineData(CompressionLevel.Fastest)]
    [InlineData(CompressionLevel.Optimal)]
    public void Create_CompressionLevelEnabled_NonBuffered_IncludesGzipRequestBodyHandler(CompressionLevel level)
    {
        var options = new ValidDsnOptions
        {
            RequestBodyCompressionLevel = level,
            RequestBodyCompressionBuffered = false
        };
        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        Assert.Contains(client.GetMessageHandlers(), h => h.GetType() == typeof(GzipRequestBodyHandler));
    }

    [Fact]
    public void Create_RetryAfterHandler_FirstHandler()
    {
        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(ValidDsnOptions.Instance);

        Assert.Equal(typeof(RetryAfterHandler), client.GetMessageHandlers().First().GetType());
    }

    [Fact]
    public void Create_DefaultHeaders_AcceptJson()
    {
        var configureHandlerInvoked = false;
        var options = new ValidDsnOptions
        {
            ConfigureClient = client =>
            {
                Assert.Equal("application/json", client.DefaultRequestHeaders.Accept.ToString());
                configureHandlerInvoked = true;
            }
        };
        var sut = new DefaultSentryHttpClientFactory();

        _ = sut.Create(options);

        Assert.True(configureHandlerInvoked);
    }

    [Fact]
    public void Create_ProvidedCreateHttpClientHandler_ReturnedHandlerUsed()
    {
        var handler = Substitute.For<HttpClientHandler>();
        var options = new ValidDsnOptions
        {
            CreateHttpClientHandler = () => handler
        };
        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        Assert.Contains(client.GetMessageHandlers(), h => ReferenceEquals(handler, h));
    }

    [Fact]
    public void Create_NullCreateHttpClientHandler_HttpClientHandlerUsed()
    {
        var options = new ValidDsnOptions
        {
            CreateHttpClientHandler = null
        };
        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        Assert.Contains(client.GetMessageHandlers(), h => h.GetType() == typeof(HttpClientHandler));
    }

    [Fact]
    public void Create_NullReturnedCreateHttpClientHandler_HttpClientHandlerUsed()
    {
        var options = new ValidDsnOptions
        {
            CreateHttpClientHandler = () => null
        };
        var sut = new DefaultSentryHttpClientFactory();

        var client = sut.Create(options);

        Assert.Contains(client.GetMessageHandlers(), h => h.GetType() == typeof(HttpClientHandler));
    }
}
