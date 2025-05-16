#nullable enable

using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Tests;

public class SentryStructuredLoggerTests
{
    [Fact]
    public void SmokeTest()
    {
        string categoryName = "CategoryName";
        SentryLoggingOptions options = new()
        {
            EnableLogs = true,
        };
        IHub hub = Substitute.For<IHub>();
        hub.IsEnabled.Returns(true);
        hub.Logger.Returns(Sentry.SentryStructuredLogger.CreateDisabled(hub));

        var logger = new SentryStructuredLogger(categoryName, options, hub);

        IDisposable? disposable = logger.BeginScope("state");
        disposable.Should().NotBeNull();

        logger.IsEnabled(LogLevel.Warning).Should().BeTrue();

        EventId eventId = new(1, "eventId");
        Exception exception = new InvalidOperationException();
        Func<string, Exception?, string> formatter = (string state, Exception? exception) =>
        {
            state.Should().Be("state");
            exception.Should().BeOfType<InvalidOperationException>();
            return "Message";
        };
        logger.Log(LogLevel.Warning, eventId, "state", exception, formatter);
    }
}
