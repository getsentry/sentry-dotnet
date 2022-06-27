namespace Sentry.Tests.Infrastructure;

public class DiagnosticLoggerTests
{
    [Fact]
    public void StripsEnvironmentNewlineFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo" + Environment.NewLine + "Bar");
        Assert.Equal("  Debug: FooBar", logger.LastMessageLogged);
    }

    [Fact]
    public void StripsNewlineCharsFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo\nBar");
        Assert.Equal("  Debug: FooBar", logger.LastMessageLogged);
    }

    [Fact]
    public void StripsLinefeedCharsFromMessage()
    {
        var logger = new FakeLogger(SentryLevel.Debug);
        logger.LogDebug("Foo\rBar");
        Assert.Equal("  Debug: FooBar", logger.LastMessageLogged);
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
