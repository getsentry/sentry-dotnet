#if NET5_0_OR_GREATER && !__MOBILE__
using Sentry.Extensibility;
using Sentry.Internal;

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
// If we ever add a Windows platform target (net6.0-windows, etc.), we could refactor
// to avoid reflection (which would also allow us to support trimming with this
// integration).
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

    private IHub _hub = null!;
    private SentryOptions _options = null!;

    internal static bool IsApplicable => WinUIAssembly != null;

    public void Register(IHub hub, SentryOptions options)
    {
        if (!IsApplicable)
        {
            return;
        }

        if (AotHelper.IsTrimmed)
        {
            options.Log(SentryLevel.Info, "WinUIUnhandledExceptionIntegration Integration is disabled because trimming is enabled.");
            return;
        }

        _hub = hub;
        _options = options;

        // Hook the main event handler
        AttachEventHandler();
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
    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = AotHelper.SuppressionJustification)]
    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.", Justification = AotHelper.SuppressionJustification)]
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
            // If we get an exception we should let the user know how they can manually wire up the event handler.
            _options.LogError(ex, "Could not attach WinUIUnhandledExceptionHandler.");
        }
    }

    [UnconditionalSuppressMessage("TrimAnalyzer", "IL2075", Justification = AotHelper.SuppressionJustification)]
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
