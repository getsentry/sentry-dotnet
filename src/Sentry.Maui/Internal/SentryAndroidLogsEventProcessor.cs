using Sentry.Extensibility;

namespace Sentry.Maui.Internal;

#if ANDROID
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
        if (!_sendLogcatLogs)
        {
            return @event;
        }
        try
        {
            if (@event.Exception is null)
                return @event;

            if(_logCatIntegrationType == LogCatIntegrationType.Unhandled)
            {
                if(!@event.HasTerminalException())
                    return @event;
            }

            var process = Runtime.GetRuntime()?.Exec("logcat -t 1000 *:I");
            if (process?.InputStream is null)
                return @event;

            using var output = Android.App.Application.Context.OpenFileOutput("sentry_logcat.txt", FileCreationMode.Private);
            if (output is null)
                return @event;

            process.InputStream.CopyTo(output);
            process.WaitFor();

            hint.AddAttachment(Android.App.Application.Context.FilesDir!.Path + "/sentry_logcat.txt", AttachmentType.Default, "text/plain");

            return @event;
        }
        catch (Exception e) // Catch all exceptions to prevent crashing the app during logging
        {
            _sendLogcatLogs = false;
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
