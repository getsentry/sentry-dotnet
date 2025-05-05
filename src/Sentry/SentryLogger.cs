using Sentry.Experimental;
using Sentry.Infrastructure;
using Sentry.Protocol.Envelopes;

//TODO: add XML docs
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLogger
{
    //TODO: QUESTION: Trace vs LogTrace
    // Trace() is from the Sentry Logs feature specs. LogTrace() would be more .NET idiomatic
    public void Trace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Trace, template, parameters, configureLog);
    }

    //TODO: QUESTION: parameter name "template" vs "format"
    // "template" from the "sentry.message.template" attributes of the envelope
    // "format" as in System.String.Format to be more idiomatic
    public void Debug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Debug, template, parameters, configureLog);
    }

    public void Info(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Info, template, parameters, configureLog);
    }

    //TODO: QUESTION: Warn vs Warning
    // Warn is from the Sentry Logs feature specs. Warning would be more .NET idiomatic
    public void Warn(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Warn, template, parameters, configureLog);
    }

    public void Error(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Error, template, parameters, configureLog);
    }

    public void Fatal(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        CaptureLog(SentrySeverity.Fatal, template, parameters, configureLog);
    }

    //TODO: consider ReadOnlySpan for TFMs where Span is available
    //  or: utilize a custom [InterpolatedStringHandler] for modern TFMs
    //      with which we may not be able to enforce on compile-time to only support string, boolean, integer, double
    //      but we could have an Analyzer for that, indicating that Sentry does not support other types if used in the interpolated string
    //  or: utilize a SourceGen, similar to the Microsoft.Extensions.Logging [LoggerMessage]
    //      with which we could enforce on compile-time to only support string, boolean, integer, double
    private static void CaptureLog(SentrySeverity level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        var message = string.Format(template, parameters ?? []);
        SentryLog log = new(level, message);
        configureLog?.Invoke(log);

        var hub = SentrySdk.CurrentHub;
        _ = hub.CaptureEnvelope(Envelope.FromLog(log));
    }
}
