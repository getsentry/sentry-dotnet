using System;
using System.Net;
using Sentry.Http;
using Xunit;

namespace Sentry.Tests.Http
{
    public class HttpOptionsTests
    {
        [Fact]
        public void Ctor_NullSentryUrl_ThrowsArgumentNullException()
        {
            var arg = Assert.Throws<ArgumentNullException>(() => new HttpOptions(null));
            Assert.Equal("sentryUri", arg.ParamName);
        }

        [Fact]
        public void Ctor_RelativeSentryUrl_ThrowsArgumentException()
        {
            var arg = Assert.Throws<ArgumentException>(() => new HttpOptions(new Uri("/store/123123", UriKind.Relative)));
            Assert.Equal("sentryUri", arg.ParamName);
            Assert.Equal(@"URL to Sentry must be absolute. Example: https://98718479@sentry.io/123456
Parameter name: sentryUri", arg.Message);
        }

        [Fact]
        public void SentryUri_ValidUrl_StoredInProperty()
        {
            var sut = new HttpOptions(DsnSamples.Valid.SentryUri);
            Assert.Same(DsnSamples.Valid.SentryUri, sut.SentryUri);
        }

        [Fact]
        public void DecompressionMethods_ByDefault_AllBitsSet()
        {
            var sut = new HttpOptions(DsnSamples.Valid.SentryUri);
            Assert.Equal(~DecompressionMethods.None, sut.DecompressionMethods);
        }

#if DEBUG
        [Fact]
        public void HandleFailedEventSubmission_ByDefault_HandlerAssigned()
        {
            var sut = new HttpOptions(DsnSamples.Valid.SentryUri);
            Assert.NotNull(sut.HandleFailedEventSubmission);
        }
#else
        [Fact]
        public void HandleFailedEventSubmission_ByDefault_NoHandlerAssigned()
        {
            var sut = new HttpOptions(DsnSamples.Valid.SentryUri);
            Assert.Null(sut.HandleFailedEventSubmission);
        }
#endif
    }
}
