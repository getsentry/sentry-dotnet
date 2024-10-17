namespace Sentry.Testing;

/// <summary>
/// We most frequently call IDiagnosticLogger via extension methods on the SentryOptions class, which obscures what
/// we're expecting to receive via mocks when writing tests. This class provides some test extensions that mirror the
/// extensions provided for SentryOptions to make it easier to write tests that match the code they're testing.
/// </summary>
public static class SentryOptionsExtensions
{
    private static SentryOptions DidNotReceiveReceiveLog(this SentryOptions substitute, SentryLevel level)
    {
        substitute.DiagnosticLogger.DidNotReceive().Log(level, Arg.Any<string>(), null, Arg.Any<object[]>());
        return substitute;
    }

    private static SentryOptions DidNotReceiveReceiveLog(this SentryOptions substitute, SentryLevel level, string message, params object[] args)
    {
        substitute.DiagnosticLogger.DidNotReceive().Log(level, message, null, args);
        return substitute;
    }

    private static SentryOptions ReceivedLog(this SentryOptions substitute, SentryLevel level)
    {
        substitute.DiagnosticLogger.Received().Log(level, Arg.Any<string>(), null, Arg.Any<object[]>());
        return substitute;
    }

    private static SentryOptions ReceivedLog(this SentryOptions substitute, SentryLevel level, string message, params object[] args)
    {
        substitute.DiagnosticLogger.Received().Log(level, message, null, args);
        return substitute;
    }

    public static SentryOptions ReceivedLogDebug(this SentryOptions substitute)
        => ReceivedLog(substitute, SentryLevel.Debug);

    public static SentryOptions ReceivedLogDebug(this SentryOptions substitute, string message, params object[] args)
        => ReceivedLog(substitute, SentryLevel.Debug, message, args);

    public static SentryOptions ReceivedLogInfo(this SentryOptions substitute)
        => ReceivedLog(substitute, SentryLevel.Info);

    public static SentryOptions ReceivedLogInfo(this SentryOptions substitute, string message, params object[] args)
        => ReceivedLog(substitute, SentryLevel.Info, message, args);

    public static SentryOptions DidNotReceiveReceiveLogInfo(this SentryOptions substitute)
        => DidNotReceiveReceiveLog(substitute, SentryLevel.Info);

    public static SentryOptions DidNotReceiveReceiveLogInfo(this SentryOptions substitute, string message, params object[] args)
        => DidNotReceiveReceiveLog(substitute, SentryLevel.Info, message, args);

    public static SentryOptions ReceivedLogWarning(this SentryOptions substitute)
        => ReceivedLog(substitute, SentryLevel.Warning);

    public static SentryOptions ReceivedLogWarning(this SentryOptions substitute, string message, params object[] args)
        => ReceivedLog(substitute, SentryLevel.Warning, message, args);

    public static SentryOptions ReceivedLogError(this SentryOptions substitute)
        => ReceivedLogError(substitute, string.Empty);

    public static SentryOptions ReceivedLogError(this SentryOptions substitute, string message, params object[] args)
        => ReceivedLog(substitute, SentryLevel.Error, message, args);

    public static SentryOptions ReceivedLogError(this SentryOptions substitute, Exception exception, string message,
        params object[] args)
    {
        substitute.DiagnosticLogger.Received().Log(SentryLevel.Error, message, exception, Arg.Any<object[]>());
        return substitute;
    }
}
