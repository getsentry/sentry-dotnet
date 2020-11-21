using System.IO.Compression;
using System.Net;
using Sentry.Internal;
using Sentry.PlatformAbstractions;
using Xunit;

namespace Sentry.Tests
{
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

#if NETFX
        [SkippableFact]
        public void StackTraceFactory_RunningOnMono_HasMonoStackTraceFactory()
        {
            Skip.If(!RuntimeInfo.GetRuntime().IsMono());

            var sut = new SentryOptions();
            Assert.IsType<MonoSentryStackTraceFactory>(sut.SentryStackTraceFactory);
        }

        [SkippableFact]
        public void StackTraceFactory_NotRunningOnMono_NotMonoStackTraceFactory()
        {
            Skip.If(RuntimeInfo.GetRuntime().IsMono());

            var sut = new SentryOptions();
            Assert.IsNotType<MonoSentryStackTraceFactory>(sut.SentryStackTraceFactory);
        }
#endif
    }
}
