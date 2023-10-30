#if NET5_0_OR_GREATER
using Sentry.Extensibility;

namespace Sentry.Integrations;

// This integration hooks unhandled exceptions in WinUI 3.
// The primary hook is Microsoft.UI.Xaml.Application.Current.UnhandledException
//
// There are some quirks to be aware of:
//
// - By default, important details (message, stack trace, etc.) are stripped away.
//   We can work around this by catching first-chance exceptions.
//   See: https://github.com/microsoft/microsoft-ui-xaml/issues/7160
//
// - Exceptions from background threads are not caught here.
//   However, they are caught by System.AppDomain.CurrentDomain.UnhandledException,
//   which we already hook in our AppDomainUnhandledExceptionIntegration
//   See: https://github.com/microsoft/microsoft-ui-xaml/issues/5221
//
// Note that we use reflection in this integration to get at WinUI code.
// If we ever add a Windows platform target (net6.0-windows, etc.), we could refactor to avoid reflection.
//
// This integration is for WinUI 3.  It does NOT work for UWP (WinUI 2).
// For UWP, the calling application will need to hook the event handler.
// See https://docs.sentry.io/platforms/dotnet/guides/uwp/
// (We can't do it automatically without a separate UWP class library,
// due to a security exception when attempting to attach the event dynamically.)

internal class WinUIUnhandledExceptionIntegration : ISdkIntegration
{
    private static readonly byte[] WinUIPublicKeyToken = Convert.FromHexString("de31ebe4ad15742b");
    private static readonly Assembly? WinUIAssembly = GetWinUIAssembly();

    private Exception? _lastFirstChanceException;
    private IHub _hub = null!;
    private SentryOptions _options = null!;

    internal static bool IsApplicable => WinUIAssembly != null;

    public void Register(IHub hub, SentryOptions options)
    {
        if (!IsApplicable)
        {
            return;
        }

        _hub = hub;
        _options = options;

        // Hook the main event handler
        try
        {
            AttachEventHandler();
        }
        catch
        {
            // When compiling AOT applications an exception will be thrown, in which case we log a message to let the
            // SDK user know how they can resolve the issue and then we ignore...
            // TODO: We need to create a mechanism for users to wire this up manually and document this in a separate PR
            _options.LogDebug("Could not attach UnhandledExceptionHandler automatically. You'll need to do this manually: TODO - link to docs");
        }

        // First part of workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/7160
        AppDomain.CurrentDomain.FirstChanceException += (_, e) => _lastFirstChanceException = e.Exception;
    }

    private static Assembly? GetWinUIAssembly()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Not running on Windows
            return null;
        }

        // Attempt to locate the Microsoft.WinUI assembly from the AppDomain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        return Array.Find(assemblies, x =>
        {
            // check by name and public key token
            var assemblyName = x.GetName();
            return assemblyName.Name == "Microsoft.WinUI" &&
                   assemblyName.GetPublicKeyToken()?.SequenceEqual(WinUIPublicKeyToken) is true;
        });
    }

    /// <summary>
    /// This method uses reflection to hook up an UnhandledExceptionHandler. When IsTrimmed is true, users will have
    /// follow our guidance to perform this initialization manually.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code",
        Justification = "This code is only intended to work for JIT compilation and exceptions will be swallowed")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.",
        Justification = "This code is only intended to work for JIT compilation and exceptions will be swallowed")]
    private void AttachEventHandler()
    {
        try
        {
            // Reflection equivalent of:
            //   Microsoft.UI.Xaml.Application.Current.UnhandledException += WinUIUnhandledExceptionHandler;
            //
            EventHandler handler = WinUIUnhandledExceptionHandler!;
            var applicationType = WinUIAssembly!.GetType("Microsoft.UI.Xaml.Application")!;
            var application = applicationType.GetProperty("Current")!.GetValue(null);
            var eventInfo = applicationType.GetEvent("UnhandledException")!;
            var typedHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, handler.Target, handler.Method);
            eventInfo.AddEventHandler(application, typedHandler);
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Could not attach WinUIUnhandledExceptionHandler.");
        }
    }

    private void WinUIUnhandledExceptionHandler(object sender, object e)
    {
        bool handled;
        Exception exception;
        try
        {
            var eventArgsType = e.GetType();
            handled = (bool)eventArgsType.GetProperty("Handled")!.GetValue(e)!;
            exception = (Exception)eventArgsType.GetProperty("Exception")!.GetValue(e)!;
        }
        catch (Exception ex)
        {
            _options.LogError(ex, "Could not get exception details in WinUIUnhandledExceptionHandler.");
            return;
        }

        // Second part of workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/7160
        if (exception.StackTrace is null)
        {
            exception = _lastFirstChanceException!;
        }

        // Set some useful data and capture the exception
        var description = "This exception was caught by the Windows UI global error handler.";
        if (!handled)
        {
            description += " The application likely crashed as a result of this exception.";
        }

        exception.SetSentryMechanism("Microsoft.UI.Xaml.UnhandledException", description, handled);

        // Call the internal implementation, so that we still capture even if the hub has been disabled.
        _hub.CaptureExceptionInternal(exception);

        if (!handled)
        {
            // We're crashing, so flush events to Sentry right away
            _hub.Flush(_options.ShutdownTimeout);
        }
    }
}

#endif
