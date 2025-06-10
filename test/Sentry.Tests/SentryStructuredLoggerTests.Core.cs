#nullable enable

namespace Sentry.Tests;

public partial class SentryStructuredLoggerTests
{
    [SkippableTheory(typeof(MissingMethodException))] //throws in .NETFramework on non-Windows for System.Collections.Immutable.ImmutableArray`1
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetDefaultSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelope(envelope, level);
    }

    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetDefaultSut();

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", ["string", true, 1, 2.2], ConfigureLog);

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }
}

file static class SentryStructuredLoggerExtensions
{
    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(template, parameters, configureLog);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(template, parameters, configureLog);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(template, parameters, configureLog);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(template, parameters, configureLog);
                break;
            case SentryLogLevel.Error:
                logger.LogError(template, parameters, configureLog);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(template, parameters, configureLog);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
