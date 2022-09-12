using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests;

public class SentryAspNetOptionsExtensionsTests :
    HttpContextTest
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
