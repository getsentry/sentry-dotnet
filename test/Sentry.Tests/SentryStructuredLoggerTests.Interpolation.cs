#nullable enable

namespace Sentry.Tests;

#if NET6_0_OR_GREATER
public partial class SentryStructuredLoggerTests
{
    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_InterpolatedStringHandler_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, $"Template string with arguments: {"string"}, {true}, {1}, {2.2}");
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        _fixture.AssertEnvelopeWithoutAttributes(envelope, level);
    }

    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_InterpolatedStringHandler_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, $"Template string with arguments: {"string"}, {true}, {1}, {2.2}");
        logger.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }

    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_InterpolatedStringHandler_ConfigureLog_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, ConfigureLog, $"Template string with arguments: {"string"}, {true}, {1}, {2.2}");
        logger.Flush();

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
    public void Log_InterpolatedStringHandler_ConfigureLog_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, ConfigureLog, $"Template string with arguments: {"string"}, {true}, {1}, {2.2}");
        logger.Flush();

        _fixture.Hub.Received(0).CaptureEnvelope(Arg.Any<Envelope>());
    }
}

file static class SentryStructuredLoggerExtensions
{
    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, [InterpolatedStringHandlerArgument(nameof(logger))] ref SentryStructuredLogger.LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(ref handler, template);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(ref handler, template);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(ref handler, template);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(ref handler, template);
                break;
            case SentryLogLevel.Error:
                logger.LogError(ref handler, template);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(ref handler, template);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, Action<SentryLog> configureLog, [InterpolatedStringHandlerArgument(nameof(logger))] ref SentryStructuredLogger.LogInterpolatedStringHandler handler, [CallerArgumentExpression(nameof(handler))] string template = "")
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(configureLog, ref handler, template);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(configureLog, ref handler, template);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(configureLog, ref handler, template);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(configureLog, ref handler, template);
                break;
            case SentryLogLevel.Error:
                logger.LogError(configureLog, ref handler, template);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(configureLog, ref handler, template);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
#endif
