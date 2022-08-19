#if NET5_0_OR_GREATER
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Sentry.Integrations
{
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
        private static readonly Assembly? WinUIAssembly = GetWinUIAssembly();

        private Exception? _lastFirstChanceException;
        private IHub _hub = null!;
        private SentryOptions _options = null!;

        public static bool IsApplicable => WinUIAssembly != null;

        public void Register(IHub hub, SentryOptions options)
        {
            if (!IsApplicable)
            {
                return;
            }

            _hub = hub;
            _options = options;

            // Hook the main event handler
            AttachEventHandler();

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
                // first check by name
                var assemblyName = x.GetName();
                if (assemblyName.Name != "Microsoft.WinUI")
                {
                    return false;
                }

                // check the public key token also
                var token = assemblyName.GetPublicKeyToken();
                return token != null && string.Equals(Convert.ToHexString(token), "de31ebe4ad15742b", StringComparison.OrdinalIgnoreCase);
            });
        }

        private void AttachEventHandler()
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

        private void WinUIUnhandledExceptionHandler(object sender, object e)
        {
            var eventArgsType = e.GetType();
            var handled = (bool)eventArgsType.GetProperty("Handled")!.GetValue(e)!;
            var exception = (Exception)eventArgsType.GetProperty("Exception")!.GetValue(e)!;

            // Second part of workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/7160
            if (exception.StackTrace is null)
            {
                exception = _lastFirstChanceException!;
            }

            // Set some useful data and capture the exception
            exception.Data[Protocol.Mechanism.HandledKey] = handled;
            exception.Data[Protocol.Mechanism.MechanismKey] = "Microsoft.UI.Xaml.UnhandledException";
            _hub.CaptureException(exception);

            if (!handled)
            {
                // We're crashing, so flush events to Sentry right away
                _hub.FlushAsync(_options.ShutdownTimeout).GetAwaiter().GetResult();
            }
        }
    }
}
#endif
