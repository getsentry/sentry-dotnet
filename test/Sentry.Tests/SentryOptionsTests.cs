using System.IO.Compression;
using System.Net;
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

#if NETFRAMEWORK
    [SkippableFact(typeof(IsTypeException))]
    public void StackTraceFactory_RunningOnMono_HasMonoStackTraceFactory()
    {
        Skip.If(!RuntimeInfo.GetRuntime().IsMono());

        var sut = new SentryOptions();
        Assert.IsType<MonoSentryStackTraceFactory>(sut.SentryStackTraceFactory);
    }

    [SkippableFact(typeof(IsNotTypeException))]
    public void StackTraceFactory_NotRunningOnMono_NotMonoStackTraceFactory()
    {
        Skip.If(RuntimeInfo.GetRuntime().IsMono());

        var sut = new SentryOptions();
        Assert.IsNotType<MonoSentryStackTraceFactory>(sut.SentryStackTraceFactory);
    }
#endif
}
