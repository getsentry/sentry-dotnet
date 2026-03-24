using ObjCRuntime;
using Sentry.Extensibility;
using Sentry.Integrations;

namespace Sentry.Cocoa;

/// <summary>
/// When AOT Compiling iOS applications, the AppDomain UnhandledExceptionHandler doesn't fire. So instead we intercept
/// the Runtime.RuntimeMarshalManagedException event.
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
            ex.SetSentryMechanism(
                "Runtime.MarshalManagedException",
                "This exception was caught by the .NET Runtime Marshal Managed Exception global error handler. " +
                "The application may have crashed as a result of this exception.",
                handled: false);

            // Call the internal implementation, so that we still capture even if the hub has been disabled.
            _hub?.CaptureExceptionInternal(ex);

            // This is likely a terminal exception so try to send the crash report before shutting down
            _hub?.Flush();

            // Under Mono, Disable/UnwindNativeCode call mono_raise_exception which can unwind into
            // a managed catch and never hit abort(). The thread-local ignore flag would then linger
            // and silently swallow an unrelated later SIGABRT on this thread, so skip arming it. See
            // https://github.com/dotnet/macios/blob/be8a2ca1057242f745ef58011a02ffe21326d180/runtime/runtime.m#L2215
            var isMono = Type.GetType("Mono.Runtime") != null;
            if (isMono && e.ExceptionMode is MarshalManagedExceptionMode.Disable
                                          or MarshalManagedExceptionMode.UnwindNativeCode)
            {
                return;
            }

            // Otherwise the runtime will call abort() after we return — directly via
            // xamarin_assertion_message, or indirectly via the uncaught-NSException handler for
            // ThrowObjectiveCException. Tell SentryCrash to ignore that SIGABRT so we don't emit a
            // duplicate native crash for an exception we've already captured. See
            // https://github.com/dotnet/macios/blob/be8a2ca1057242f745ef58011a02ffe21326d180/runtime/runtime.m#L2285
            const int SIGABRT = 6;
            SentryCocoaHybridSdk.IgnoreNextSignal(SIGABRT);
        }
    }
}
