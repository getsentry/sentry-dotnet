using System.IO.Compression;
using System.Net;
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

        [Fact]
        public void HandleFailedEventSubmission_ByDefault_NoHandlerAssigned()
        {
            var sut = new SentryOptions();
            Assert.Null(sut.HandleFailedEventSubmission);
        }
    }
}
