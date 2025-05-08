using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;

namespace Sentry;

/// <summary>
/// Creates and sends logs to Sentry.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLogger
{
    private readonly RandomValuesFactory _randomValuesFactory;

    internal SentryLogger()
    {
        _randomValuesFactory = new SynchronizedRandomValuesFactory();
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Trace"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    public void Trace(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Trace, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Debug"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Debug(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Debug, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Info"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Info(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Info, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Warn"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Warn(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Warn, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Error"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void Error(string template, object[]? parameters = null, Action<SentryLog>? configureLog = null)
    {
        if (IsEnabled())
        {
            CaptureLog(SentrySeverity.Error, template, parameters, configureLog);
        }
    }

    /// <summary>
    /// Creates and sends a structured log to Sentry, with severity <see cref="SentrySeverity.Fatal"/>, when enabled and sampled.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
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

    private void CaptureLog(SentrySeverity level, string template, object[]? parameters, Action<SentryLog>? configureLog)
    {
        var timestamp = DateTimeOffset.UtcNow;

        var hub = SentrySdk.CurrentHub;

        if (hub.GetSentryOptions() is not { EnableLogs: true } options)
        {
            return;
        }

        if (!_randomValuesFactory.NextBool(options.LogsSampleRate))
        {
            return;
        }

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
            //TODO: change to Diagnostic Logger (if enabled)
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            Console.WriteLine(e);
            return;
        }

        if (configuredLog is not null)
        {
            //TODO: enqueue in Batch-Processor / Background-Worker
            // see https://github.com/getsentry/sentry-dotnet/issues/4132
            _ = hub.CaptureEnvelope(Envelope.FromLog(configuredLog));
        }
    }
}
