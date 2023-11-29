#if ANDROID
using Sentry.Extensibility;
using Runtime = Java.Lang.Runtime;
using Android.Content;
using Application = Android.App.Application;

namespace Sentry.Maui.Internal;

internal class SentryAndroidLogsEventProcessor : ISentryEventProcessorWithHint
{
    private static bool _sendLogcatLogs = true;
    private readonly LogCatIntegrationType _logCatIntegrationType;

    public SentryAndroidLogsEventProcessor(LogCatIntegrationType logCatIntegrationType)
    {
        _logCatIntegrationType = logCatIntegrationType;
    }

    public SentryEvent? Process(SentryEvent @event, Hint hint)
    {
        // If sending has failed once, we have to disable this feature to prevent infinite loops and to allow the SDK to work otherwise
        if (!_sendLogcatLogs)
        {
            return @event;
        }

        // The logcat command only works on Android API 23 and above
        if (!OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            _sendLogcatLogs = false;
            return @event;
        }

        try
        {
            if (_logCatIntegrationType != LogCatIntegrationType.All && @event.Exception is null)
                return @event;

            // Only send logcat logs if the event is unhandled if the integration is set to Unhandled
            if(_logCatIntegrationType == LogCatIntegrationType.Unhandled)
            {
                if(!@event.HasTerminalException())
                    return @event;
            }

            // We run the logcat command via the runtime
            var process = Runtime.GetRuntime()?.Exec("logcat -t 1000 *:I");

            // Strangely enough, process.InputStream is the *output* of the command
            if (process?.InputStream is null)
                return @event;

            // We write the logcat logs to a file so we can attach it
            using var output = Application.Context.OpenFileOutput("sentry_logcat.txt", FileCreationMode.Private);
            if (output is null)
                return @event;

            process.InputStream.CopyTo(output);
            process.WaitFor();

            hint.AddAttachment(Application.Context.FilesDir!.Path + "/sentry_logcat.txt", AttachmentType.Default, "text/plain");

            return @event;
        }
        catch (Exception e) // Catch all exceptions to prevent crashing the app during logging
        {
            // Disable the feature if it fails once
            _sendLogcatLogs = false;

            // Log the failure to Sentry
            SentrySdk.CaptureException(e);
            return @event;
        }
    }

    public SentryEvent? Process(SentryEvent @event)
    {
        return Process(@event, new Hint());
    }
}
#endif
