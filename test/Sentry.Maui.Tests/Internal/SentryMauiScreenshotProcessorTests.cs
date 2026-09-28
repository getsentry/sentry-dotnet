using Sentry.Maui.Internal;

namespace Sentry.Maui.Tests.Internal;

public class SentryMauiScreenshotProcessorTests
{
    [Fact]
    public void Process_BeforeScreenshotCaptureThrows_KeepsEventAndSkipsScreenshot()
    {
        // Arrange
        var exception = new InvalidOperationException("callback failed");
        var logger = new InMemoryDiagnosticLogger();
        var options = new SentryMauiOptions
        {
            Debug = true,
            DiagnosticLogger = logger
        };
        options.SetBeforeScreenshotCapture((_, _) => throw exception);
        var processor = new SentryMauiScreenshotProcessor(options);

        var @event = new SentryEvent();
        var hint = new SentryHint();

        // Act
        var processed = processor.Process(@event, hint);

        // Assert
        processed.Should().BeSameAs(@event);
        hint.Attachments.Should().BeEmpty();
        logger.Entries.Should().ContainSingle(entry =>
            entry.Level == SentryLevel.Error &&
            entry.Exception == exception &&
            entry.Message == "BeforeScreenshotCapture callback failed.");
    }

    [Fact]
    public void Process_BeforeScreenshotCaptureReturnsTrue_AddsScreenshot()
    {
        // Arrange
        var options = new SentryMauiOptions();
        options.SetBeforeScreenshotCapture((_, _) => true);
        var processor = new SentryMauiScreenshotProcessor(options);

        var hint = new SentryHint();

        // Act
        processor.Process(new SentryEvent(), hint);

        // Assert
        hint.Attachments.Should().ContainSingle(a => a.FileName == "screenshot.jpg");
    }
}
