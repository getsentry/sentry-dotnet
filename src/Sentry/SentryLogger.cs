using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;
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
    private readonly RandomValuesFactory _randomValuesFactory;

    internal SentryLogger()
    {
        _randomValuesFactory = new SynchronizedRandomValuesFactory();
    }

    //TODO: QUESTION: Trace vs LogTrace
    // Trace() is from the Sentry Logs feature specs. LogTrace() would be more .NET idiomatic
    public void Trace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Trace, template, parameters, configureLog);
        }
    }

    //TODO: QUESTION: parameter name "template" vs "format"
    // "template" from the "sentry.message.template" attributes of the envelope
    // "format" as in System.String.Format to be more idiomatic
    public void Debug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Debug, template, parameters, configureLog);
        }
    }

    public void Info(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Info, template, parameters, configureLog);
        }
    }

    //TODO: QUESTION: Warn vs Warning
    // Warn is from the Sentry Logs feature specs. Warning would be more .NET idiomatic
    public void Warn(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Warn, template, parameters, configureLog);
        }
    }

    public void Error(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Error, template, parameters, configureLog);
        }
    }

    public void Fatal(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Fatal, template, parameters, configureLog);
        }
    }

    private bool IsEnabled()
    {
        var hub = SentrySdk.CurrentHub;

        if (hub.GetSentryOptions() is { } options)
        {
            return options.EnableLogs;
        }

        return false;
    }

    //TODO: consider ReadOnlySpan for TFMs where Span is available
    //  or: utilize a custom [InterpolatedStringHandler] for modern TFMs
    //      with which we may not be able to enforce on compile-time to only support string, boolean, integer, double
    //      but we could have an Analyzer for that, indicating that Sentry does not support other types if used in the interpolated string
    //  or: utilize a SourceGen, similar to the Microsoft.Extensions.Logging [LoggerMessage]
    //      with which we could enforce on compile-time to only support string, boolean, integer, double
    private void CaptureLog(SentrySeverity level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var hub = SentrySdk.CurrentHub;

        if (hub.GetSentryOptions() is not { EnableLogs: true } options)
        {
            //Logs disabled
            return;
        }

        if (!_randomValuesFactory.NextBool(options.LogsSampleRate))
        {
            //Log sampled
            return;
        }

        //process log (attach attributes)

        var scopeManager = (hub as Hub)?.ScopeManager;
        SentryId traceId;
        if (hub.GetSpan() is { } span)
        {
            traceId = span.TraceId;
        }
        else if (scopeManager is not null)
        {
            var currentScope = scopeManager.GetCurrent().Key;
            traceId = currentScope.PropagationContext.TraceId;
        }
        else
        {
            traceId = SentryId.Empty;
        }

        var message = string.Format(template, parameters ?? []);
        SentryLog log = new(timestamp, traceId, level, message)
        {
            Template = template,
            Parameters = parameters,
        };
        log.SetAttributes(hub, scopeManager, options);

        SentryLog? configuredLog;
        try
        {
            configureLog?.Invoke(log);
            configuredLog = options.BeforeSendLogInternal?.Invoke(log);
        }
        catch (Exception e)
        {
            //TODO: diagnostic log
            Console.WriteLine(e);
            return;
        }

        if (configuredLog is not null)
        {
            //TODO: enqueue in Batch-Processor / Background-Worker
            _ = hub.CaptureEnvelope(Envelope.FromLog(configuredLog));
        }
    }
}
