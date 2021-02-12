using System.Linq;
using Sentry.AspNet.Internal;
using Sentry.Extensibility;
using Xunit;

namespace Sentry.AspNet.Tests
{
    public class SentryAspNetOptionsExtensionsTests
    {
        [Fact]
        public void AddAspNet_EventProcessorsContainBodyExtractor()
        {
            // Arrange
            var sut = new SentryOptions();

            // Act
            sut.AddAspNet();

            // Assert
            var processor = sut.EventProcessors?.OfType<SystemWebRequestEventProcessor>().FirstOrDefault();
            Assert.NotNull(processor);

            var extractor = Assert.IsType<RequestBodyExtractionDispatcher>(processor.PayloadExtractor);
            Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(FormRequestPayloadExtractor));
            Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(DefaultRequestPayloadExtractor));
        }
    }
}
