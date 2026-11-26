#if NET8_0_OR_GREATER && PLATFORM_NEUTRAL
using Sentry.Native;

namespace Sentry.Tests.Platforms.Native;

public class CFunctionsTests
{
    [Fact]
    public void GetProxyUrl_Credentials_Escaped()
    {
        var url = C.GetProxyUrl(new Uri("http://proxy:8080"), new NetworkCredential("us:er%41@corp", "p:ss/w%rd"));

        Assert.Equal("http://us%3Aer%2541%40corp:p%3Ass%2Fw%25rd@proxy:8080/", url);
    }

    [Fact]
    public void GetProxyUrl_NoCredentials_ReturnsAddress()
    {
        var url = C.GetProxyUrl(new Uri("http://proxy:8080"), null);

        Assert.Equal("http://proxy:8080/", url);
    }

    [Fact]
    public void GetProxyUrl_EmptyUserName_ReturnsAddress()
    {
        var url = C.GetProxyUrl(new Uri("http://proxy:8080"), new NetworkCredential("", "password"));

        Assert.Equal("http://proxy:8080/", url);
    }
}
#endif
