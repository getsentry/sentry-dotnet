#if NETFRAMEWORK
using Sentry.PlatformAbstractions;
using Xunit.Sdk;
#endif

namespace Sentry.Tests;

public class SentryOptionsTests
{
    [Fact]
    public void DecompressionMethods_ByDefault_AllBitsSet()
    {
        var sut = new SentryOptions();
        Assert.Equal(~DecompressionMethods.None, sut.DecompressionMethods);
    }

    [Fact]
    public void RequestBodyCompressionLevel_ByDefault_Optimal()
    {
        var sut = new SentryOptions();
        Assert.Equal(CompressionLevel.Optimal, sut.RequestBodyCompressionLevel);
    }

    [Fact]
    public void Transport_ByDefault_IsNull()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.Transport);
    }

    [Fact]
    public void AttachStackTrace_ByDefault_True()
    {
        var sut = new SentryOptions();
        Assert.True(sut.AttachStacktrace);
    }
}
