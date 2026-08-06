using System.Runtime.InteropServices;

namespace Sentry.Maui.Device.IntegrationTestApp;

internal static class Native
{
    private const string NativeLibrary = "__Internal";

    public static void TriggerUnmanagedThreadCrash()
    {
        var result = sentry_dotnet_trigger_unmanaged_thread_crash();
        if (result != 0)
        {
            throw new InvalidOperationException($"pthread_create failed with errno {result}.");
        }
    }

    [DllImport(NativeLibrary)]
    private static extern int sentry_dotnet_trigger_unmanaged_thread_crash();
}
