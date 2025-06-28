using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;
namespace Sentry.OpenTelemetry;

internal class OpenTelemetryExceptionListener : IDisposable
{
    private readonly IDiagnosticLogger? _logger;
    private readonly ActivityListener _listener;

    public OpenTelemetryExceptionListener(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _listener = new ActivityListener
        {
            // Ideally, we'd mimic the behavior of the OpenTelemetry ActivityListener here:
            // https://github.com/open-telemetry/opentelemetry-dotnet/blob/20988528fdb3f5689f07c9e01c99503e5fe17922/src/OpenTelemetry/Trace/TracerProviderSdk.cs#L256-L280
            // However, the TracerProviderBuilderSdk that contains the list of sources is internal. So we're forced to
            // listen to all sources :-(
            ShouldListenTo = _ => true,

            // Called when an activity is started to decide whether to enable data collection
            Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllDataAndRecorded,
#if NET9_0_OR_GREATER
            ExceptionRecorder = OnExceptionRecorded
#endif
        };
        ActivitySource.AddActivityListener(_listener);
    }

    private void OnExceptionRecorded(Activity activity, Exception exception, ref TagList tags)
    {
        activity.Exceptions().Add(new(DateTimeOffset.UtcNow, exception));
    }

    public Exception? GetEventException(Activity activity, string exceptionType, string message,
        DateTimeOffset eventTimestamp)
    {
#if NET9_0_OR_GREATER
        if (GetFullException(activity, exceptionType, message, eventTimestamp) is { } fullException)
        {
            return fullException;
        }
#endif
        // At the moment, OTEL only gives us `exception.type`, `exception.message`, and `exception.stacktrace`...
        // So the best we can do is a poor man's exception (no accurate symbolication or anything)
        try
        {
            if (CreatePoorMansException(exceptionType, message) is { } poorMansException)
            {
                return poorMansException;
            }
        }
        catch
        {
            _logger?.LogError($"Failed to create poor man's exception for type : {exceptionType}");
        }
        return null;
    }

    private static Exception? GetFullException(Activity activity, string exceptionType, string message,
        DateTimeOffset eventTimestamp) =>
        activity.Exceptions().LastOrDefault(candidate =>
            candidate.Exception.GetType().FullName == exceptionType &&
            candidate.Exception.Message == message &&
            candidate.Timestamp <= eventTimestamp)?.Exception;

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = AotHelper.AvoidAtRuntime)]
    private Exception? CreatePoorMansException(string exceptionType, string message)
    {
        if (AotHelper.IsTrimmed)
        {
            _logger?.LogWarning($"Unable to create poor man's exception with trimming enabled : {exceptionType}");
            return null;
        }

        var type = Type.GetType(exceptionType)!;
        var exception = (Exception)Activator.CreateInstance(type, message)!;
        exception.SetSentryMechanism("SentrySpanProcessor.ErrorSpan");
        return exception;
    }

    public void Dispose()
    {
        _listener.Dispose();
    }
}

internal record ActivityEventException(DateTimeOffset Timestamp, Exception Exception);

file static class ExceptionListenerExtensions
{
    public static List<ActivityEventException> Exceptions(this Activity activity)
    {
        if (activity.GetFused<List<ActivityEventException>>() is not { } exceptions)
        {
            exceptions = [];
            activity.SetFused(exceptions);
        }
        return exceptions;
    }
}
