namespace Sentry.Tests.Infrastructure;

public class DiagnosticLoggerTests
{
    [Fact]
    public void StripsEnvironmentNewlineFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo" + Environment.NewLine + "Bar" + Environment.NewLine);
        Assert.Equal("  Debug: Foo Bar", logger.LastMessageLogged);
    }

    [Fact]
    public void StripsNewlineCharsFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo\nBar\n");
        Assert.Equal("  Debug: Foo Bar", logger.LastMessageLogged);
    }

    [Fact]
    public void StripsLinefeedCharsFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo\rBar\r");
        Assert.Equal("  Debug: Foo Bar", logger.LastMessageLogged);
    }

    [Fact]
    public void StripsComboCharsFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo\r\nBar\r\n");
        Assert.Equal("  Debug: Foo Bar", logger.LastMessageLogged);
    }

    private class FakeLogger : DiagnosticLogger
    {
        public FakeLogger(SentryLevel minimalLevel) : base(minimalLevel)
        {
        }

        public string LastMessageLogged { get; private set; }

        protected override void LogMessage(string message) => LastMessageLogged = message;
    }
}
