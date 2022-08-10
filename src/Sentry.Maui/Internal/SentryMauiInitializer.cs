using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using Sentry.Protocol;

namespace Sentry.Maui.Internal;

internal class SentryMauiInitializer : IMauiInitializeService
{
    private Exception? _lastFirstChanceException;

    public void Initialize(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<SentryMauiOptions>>().Value;
        var disposer = services.GetRequiredService<Disposer>();

#if ANDROID
        var context = global::Android.App.Application.Context;
        var disposable = SentrySdk.Init(context, options);
#else
        var disposable = SentrySdk.Init(options);
#endif

        // Register the return value from initializing the SDK with the disposer.
        // This will ensure that it gets disposed when the service provider is disposed.
        // TODO: re-evaluate this with respect to MAUI app lifecycle events
        disposer.Register(disposable);

        // Bind MAUI events
        var binder = services.GetRequiredService<MauiEventsBinder>();
        binder.BindMauiEvents();

        // Register with the WinUI unhandled exception handler when needed
        RegisterApplicationUnhandledExceptionForWinUI();
    }

    private void RegisterApplicationUnhandledExceptionForWinUI()
    {
        // We need to manually attach to the unhandled exception handler on Windows
        // Note that stack traces will be empty until the following issue is resolved:
        // https://github.com/microsoft/microsoft-ui-xaml/issues/7160

        // We'll do this at runtime via reflection so that we don't have to specifically
        // build a target Windows just for this feature.

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Not running on Windows
            return;
        }

        // Locate the Microsoft.WinUI assembly from the AppDomain
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembly = Array.Find(assemblies, x => x.GetName().Name == "Microsoft.WinUI");
        if (assembly == null)
        {
            // Not in a WinUI app
            return;
        }

        // Reflection equivalent of:
        //   Microsoft.UI.Xaml.Application.Current.UnhandledException += WinUIUnhandledExceptionHandler;
        //
        EventHandler handler = WinUIUnhandledExceptionHandler!;
        var applicationType = assembly.GetType("Microsoft.UI.Xaml.Application")!;
        var application = applicationType.GetProperty("Current")!.GetValue(null);
        var eventInfo = applicationType.GetEvent("UnhandledException")!;
        var typedHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType!, handler.Target, handler.Method);
        eventInfo.AddEventHandler(application, typedHandler);

        // Workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/7160
        AppDomain.CurrentDomain.FirstChanceException += (_, e) => _lastFirstChanceException = e.Exception;
    }

    private void WinUIUnhandledExceptionHandler(object sender, object e)
    {
        var eventArgsType = e.GetType();
        var handled = (bool)eventArgsType.GetProperty("Handled")!.GetValue(e)!;
        var exception = (Exception)eventArgsType.GetProperty("Exception")!.GetValue(e)!;

        // Workaround for https://github.com/microsoft/microsoft-ui-xaml/issues/7160
        if (exception.StackTrace is null)
        {
            exception = _lastFirstChanceException!;
        }

        CaptureUnhandledException(handled, exception, "Microsoft.UI.Xaml.Application.UnhandledException");
    }

    private static void CaptureUnhandledException(bool handled, Exception exception, string mechanism)
    {
        // Set some useful data and capture the exception
        exception.Data[Mechanism.HandledKey] = handled;
        exception.Data[Mechanism.MechanismKey] = mechanism;
        SentrySdk.CaptureException(exception);
        if (!handled)
        {
            // We're crashing, so flush events to Sentry right away
            SentrySdk.Close();
        }
    }
}
