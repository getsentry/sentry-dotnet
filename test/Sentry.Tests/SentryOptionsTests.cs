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

    [Fact]
    public void EnableTracing_Default_Null()
    {
        var sut = new SentryOptions();
        Assert.Null(sut.EnableTracing);
    }

    [Fact]
    public void IsTracingEnabled_Default_False()
    {
        var sut = new SentryOptions();
        Assert.False(sut.IsTracingEnabled);
    }

    [Fact]
    public void EnableTracing_WhenNull()
    {
        var sut = new SentryOptions
        {
            EnableTracing = null
        };

        Assert.Null(sut.EnableTracing);
        Assert.Equal(0.0, sut.TracesSampleRate);
    }

    [Fact]
    public void EnableTracing_WhenFalse()
    {
        var sut = new SentryOptions
        {
            EnableTracing = false
        };

        Assert.False(sut.EnableTracing);
        Assert.Equal(0.0, sut.TracesSampleRate);
    }

    [Fact]
    public void EnableTracing_WhenTrue()
    {
        var sut = new SentryOptions
        {
            EnableTracing = true
        };

        Assert.True(sut.EnableTracing);
        Assert.Equal(1.0, sut.TracesSampleRate);
    }
}
