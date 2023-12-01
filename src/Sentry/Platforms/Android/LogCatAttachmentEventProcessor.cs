using Sentry.Extensibility;
using Runtime = Java.Lang.Runtime;
using Android.Content;
using Application = Android.App.Application;

namespace Sentry.Android;

internal class LogCatAttachmentEventProcessor : ISentryEventProcessorWithHint
{
    private static bool SendLogcatLogs = true;
    private readonly LogCatIntegrationType _logCatIntegrationType;
    private readonly IDiagnosticLogger? _diagnosticLogger;
    private readonly int _maxLines;

    public LogCatAttachmentEventProcessor(IDiagnosticLogger? diagnosticLogger, LogCatIntegrationType logCatIntegrationType, int maxLines = 1000)
    {
        _diagnosticLogger = diagnosticLogger;
        _logCatIntegrationType = logCatIntegrationType;
        _maxLines = maxLines;
    }

    public SentryEvent Process(SentryEvent @event, Hint hint)
    {
        // If sending has failed once, we have to disable this feature to prevent infinite loops and to allow the SDK to work otherwise
        if (!SendLogcatLogs)
        {
            return @event;
        }

        // The logcat command only works on Android API 23 and above
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            SendLogcatLogs = false;
            return @event;
        }

        try
        {
            if (_logCatIntegrationType != LogCatIntegrationType.All && !@event.HasException())
            {
                return @event;
            }

            var filesDir = Application.Context.FilesDir;
            if (filesDir == null)
            {
                _diagnosticLogger?.LogWarning("LogCat: Failed to get files directory");
                SendLogcatLogs = false;
                return @event;
            }

            // Only send logcat logs if the event is unhandled if the integration is set to Unhandled
            if (_logCatIntegrationType == LogCatIntegrationType.Unhandled)
            {
                if (!@event.HasTerminalException())
                {
                    return @event;
                }
            }

            // We run the logcat command via the runtime
            var process = Runtime.GetRuntime()?.Exec($"logcat -t {_maxLines} *:I");

            // Strangely enough, process.InputStream is the *output* of the command
            if (process?.InputStream is null)
            {
                return @event;
            }

            var fileName = $"sentry_logcat_{Thread.CurrentThread.ManagedThreadId}.txt";
            // We write the logcat logs to a file so we can attach it
            using var output = Application.Context.OpenFileOutput(fileName, FileCreationMode.Private);
            if (output is null)
            {
                return @event;
            }

            process.InputStream.CopyTo(output);
            process.WaitFor();

            hint.AddAttachment($"{filesDir.Path}/{fileName}", AttachmentType.Default, "text/logcat");

            return @event;
        }
        catch (Exception e) // Catch all exceptions to prevent crashing the app during logging
        {
            // Disable the feature if it fails once
            SendLogcatLogs = false;

            // Log the failure to Sentry
            _diagnosticLogger?.LogError(e, "Failed to send logcat logs");
            return @event;
        }
    }

    public SentryEvent Process(SentryEvent @event)
    {
        return Process(@event, new Hint());
    }
}
