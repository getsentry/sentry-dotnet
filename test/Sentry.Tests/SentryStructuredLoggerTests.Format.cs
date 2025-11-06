#nullable enable

namespace Sentry.Tests;

public partial class SentryStructuredLoggerTests
{
    [Theory]
    [InlineData(SentryLogLevel.Trace)]
    [InlineData(SentryLogLevel.Debug)]
    [InlineData(SentryLogLevel.Info)]
    [InlineData(SentryLogLevel.Warning)]
    [InlineData(SentryLogLevel.Error)]
    [InlineData(SentryLogLevel.Fatal)]
    public void Log_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
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
    public void Log_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
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
    public void Log_ConfigureLog_Enabled_CapturesEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
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
    public void Log_ConfigureLog_Disabled_DoesNotCaptureEnvelope(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs.Should().BeFalse();
        var logger = _fixture.GetSut();

        logger.Log(level, ConfigureLog, "Template string with arguments: {0}, {1}, {2}, {3}", "string", true, 1, 2.2);
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
    public void Log_WithoutParameters_DoesNotAttachTemplateAttribute(SentryLogLevel level)
    {
        _fixture.Options.Experimental.EnableLogs = true;
        var logger = _fixture.GetSut();

        Envelope envelope = null!;
        _fixture.Hub.CaptureEnvelope(Arg.Do<Envelope>(arg => envelope = arg));

        logger.Log(level, "Message Text");
        logger.Flush();

        _fixture.Hub.Received(1).CaptureEnvelope(Arg.Any<Envelope>());
        var log = envelope.ShouldContainSingleLog();

        log.Level.Should().Be(level);
        log.Message.Should().Be("Message Text");
        log.Template.Should().BeNull();
        log.Parameters.Should().BeEmpty();
    }
}

file static class SentryStructuredLoggerExtensions
{
    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, string template, params object[] parameters)
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(template, parameters);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(template, parameters);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(template, parameters);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(template, parameters);
                break;
            case SentryLogLevel.Error:
                logger.LogError(template, parameters);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(template, parameters);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public static void Log(this SentryStructuredLogger logger, SentryLogLevel level, Action<SentryLog> configureLog, string template, params object[] parameters)
    {
        switch (level)
        {
            case SentryLogLevel.Trace:
                logger.LogTrace(configureLog, template, parameters);
                break;
            case SentryLogLevel.Debug:
                logger.LogDebug(configureLog, template, parameters);
                break;
            case SentryLogLevel.Info:
                logger.LogInfo(configureLog, template, parameters);
                break;
            case SentryLogLevel.Warning:
                logger.LogWarning(configureLog, template, parameters);
                break;
            case SentryLogLevel.Error:
                logger.LogError(configureLog, template, parameters);
                break;
            case SentryLogLevel.Fatal:
                logger.LogFatal(configureLog, template, parameters);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }
}
