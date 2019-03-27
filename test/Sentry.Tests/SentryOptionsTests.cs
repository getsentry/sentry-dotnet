using System.IO.Compression;
using System.Linq;
using System.Net;
using Sentry.Extensibility;
#if SYSTEM_WEB
using Sentry.Internal.Web;
#endif
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

#if SYSTEM_WEB
        [Fact]
        public void MaxRequestBodySize_ByDefault_None()
        {
            var sut = new SentryOptions();
            Assert.Equal(RequestSize.None, sut.MaxRequestBodySize);
        }

        [Fact]
        public void Ctor_EventProcessorsContainBodyExtractor()
        {
            var sut = new SentryOptions();
            var processor = sut.EventProcessors.OfType<SystemWebRequestEventProcessor>().FirstOrDefault();
            Assert.NotNull(processor);
            var extractor = Assert.IsType<RequestBodyExtractionDispatcher>(processor.PayloadExtractor);
            Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(FormRequestPayloadExtractor));
            Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(DefaultRequestPayloadExtractor));
        }
#endif
    }
}
