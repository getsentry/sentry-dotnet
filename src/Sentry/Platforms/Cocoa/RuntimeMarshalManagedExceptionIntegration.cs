using ObjCRuntime;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Cocoa;

/// <summary>
/// When AOT Compiling iOS applications, the AppDomain UnhandledExceptionHandler doesn't fire. So instead we intercept
/// the Runtime.RuntimeMarshalManagedException event.
///
/// https://learn.microsoft.com/en-us/dotnet/ios/advanced-concepts/exception-marshaling
/// </summary>
internal class RuntimeMarshalManagedExceptionIntegration : ISdkIntegration
{
    private readonly IRuntime _runtime;
    private IHub? _hub;
    private SentryOptions? _options;

    internal RuntimeMarshalManagedExceptionIntegration(IRuntime? runtime = null)
        => _runtime = runtime ?? RuntimeAdapter.Instance;

    public void Register(IHub hub, SentryOptions options)
    {
        _hub = hub;
        _options = options;
        _runtime.MarshalManagedException += Handle;
    }

    // Internal for testability
    [SecurityCritical]
    internal void Handle(object sender, MarshalManagedExceptionEventArgs e)
    {
        _options?.LogDebug("Runtime Marshal Managed Exception mode {0}", e.ExceptionMode.ToString("G"));

        if (e.Exception is { } ex)
        {
            // The Obj-C runtime is about to abort. Pre-generate an event ID for the managed exception and pass it down
            // to the Cocoa SDK  to be used for the respective SIGABORT to prevent duplicate events for the same crash.
            var eventId = SentryId.Create();
            ex.SetSentryEventId(eventId);
            SentrySdk.SetCrashEventId(eventId);

            ex.SetSentryMechanism(
                "Runtime.MarshalManagedException",
                "This exception was caught by the .NET Runtime Marshal Managed Exception global error handler. " +
                "The application may have crashed as a result of this exception.",
                handled: false);

            // Call the internal implementation, so that we still capture even if the hub has been disabled.
            _hub?.CaptureExceptionInternal(ex);

            // This is likely a terminal exception so try to send the crash report before shutting down
            _hub?.Flush();
        }
    }
}
