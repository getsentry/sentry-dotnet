using Sentry.Android;

namespace Sentry.Tests.Platforms.Android;

public class AndroidDiagnosticLoggerTests
{
    private class Fixture
    {
        public TestLogger TestLogger { get; }

        public Fixture()
        {
            TestLogger = new TestLogger(SentryLevel.Debug);
        }

        public AndroidDiagnosticLogger GetSut() => new(TestLogger);
    }

    private class TestLogger : DiagnosticLogger
    {
        public List<string> Messages { get; } = new();

        public TestLogger(SentryLevel minimalLevel)
            : base(minimalLevel)
        {
        }

        protected override void LogMessage(string message) => Messages.Add(message.Trim());
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void IsEnabled()
    {
        var androidLogger = _fixture.GetSut();
        var enabled = androidLogger.IsEnabled(JavaSdk.SentryLevel.Debug);
        Assert.True(enabled);
    }

    [Fact]
    public void Log_Simple()
    {
        var androidLogger = _fixture.GetSut();
        androidLogger.Log(JavaSdk.SentryLevel.Debug, "test", args: null);
        Assert.Contains("Debug: Android: test", _fixture.TestLogger.Messages);
    }

    [Fact]
    public void Log_WithBasicArgs()
    {
        var androidLogger = _fixture.GetSut();
        androidLogger.Log(JavaSdk.SentryLevel.Debug, "test %d, %d, %s, %tQ",
            new Java.Lang.Object[] { 1, 2, "foo", new Java.Util.Date(12345) });
        Assert.Contains("Debug: Android: test 1, 2, foo, 12345", _fixture.TestLogger.Messages);
    }

    [Fact]
    public void Log_WithObjectArgs()
    {
        var androidLogger = _fixture.GetSut();
        androidLogger.Log(JavaSdk.SentryLevel.Debug, "test {value:%d}", new Java.Lang.Object[] { 123 });
        Assert.Contains("Debug: Android: test {value:123}", _fixture.TestLogger.Messages);
    }
}
