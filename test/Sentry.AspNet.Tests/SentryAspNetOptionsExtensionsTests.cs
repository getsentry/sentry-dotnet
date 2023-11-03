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
        var processor = sut.EventProcessors.Select(x => x.Lazy.Value).OfType<SystemWebRequestEventProcessor>().FirstOrDefault();
        Assert.NotNull(processor);

        var extractor = Assert.IsType<RequestBodyExtractionDispatcher>(processor.PayloadExtractor);
        Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(FormRequestPayloadExtractor));
        Assert.Contains(extractor.Extractors, p => p.GetType() == typeof(DefaultRequestPayloadExtractor));
    }

    [Fact]
    public void AddAspNet_UsedMoreThanOnce_RegisterOnce()
    {
        var options = new SentryOptions();

        options.AddAspNet();
        options.AddAspNet();

        Assert.Single(options.EventProcessors!, x => x.Lazy.Value is SystemWebRequestEventProcessor);
    }

    [Fact]
    public void AddAspNet_UsedMoreThanOnce_LogWarning()
    {
        var options = new SentryOptions();
        var logger = new InMemoryDiagnosticLogger();

        options.DiagnosticLogger = logger;
        options.Debug = true;

        options.AddAspNet();
        options.AddAspNet();

        Assert.Single(logger.Entries, x => x.Level == SentryLevel.Warning
                                           && x.Message.Contains("Subsequent call will be ignored."));
    }
}
