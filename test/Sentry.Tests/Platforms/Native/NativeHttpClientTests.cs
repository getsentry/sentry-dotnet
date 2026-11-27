#if NET8_0_OR_GREATER && PLATFORM_NEUTRAL
using Sentry.Native;

namespace Sentry.Tests.Platforms.Native;

public class NativeHttpClientTests
{
    [Fact]
    public void AddHeader_ContentLength_Skipped()
    {
        using var request = new HttpRequestMessage { Content = new ByteArrayContent(new byte[3]) };

        NativeHttpClient.AddHeader(request, "content-length", "42");

        Assert.Equal(3, request.Content.Headers.ContentLength);
    }

    [Fact]
    public void AddHeader_ContentType_AddedToContent()
    {
        using var request = new HttpRequestMessage();

        NativeHttpClient.AddHeader(request, "content-type", "application/x-sentry-envelope");

        Assert.Equal("application/x-sentry-envelope", request.Content?.Headers.ContentType?.MediaType);
    }

    [Fact]
    public void AddHeader_ContentType_KeepsContent()
    {
        var content = new ByteArrayContent(new byte[3]);
        using var request = new HttpRequestMessage { Content = content };

        NativeHttpClient.AddHeader(request, "content-type", "application/x-sentry-envelope");

        Assert.Same(content, request.Content);
    }

    [Fact]
    public void GetHeaders_RateLimited_ReturnsRateLimits()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.TryAddWithoutValidation("X-Sentry-Rate-Limits", "60:transaction;error:organization, 2700:default:organization");
        response.Headers.TryAddWithoutValidation("Retry-After", "60");

        var headers = NativeHttpClient.GetHeaders(response).ToDictionary();

        Assert.Equal("60:transaction;error:organization, 2700:default:organization", headers["X-Sentry-Rate-Limits"]);
        Assert.Equal("60", headers["Retry-After"]);
    }
}
#endif
