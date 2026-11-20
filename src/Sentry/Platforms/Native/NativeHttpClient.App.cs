using Sentry.Extensibility;

namespace Sentry.Native;

internal sealed partial class NativeHttpClient
{
    private readonly SentryOptions _options;

    // sentry_transport_new_default() takes no arguments, so set before sentry_options_new()
    internal static SentryOptions? DefaultOptions { get; set; }

    private NativeHttpClient(SentryOptions options)
    {
        _options = options;
    }

    private partial HttpClient CreateHttpClient() => _options.GetHttpClient();

    private partial Stream OpenFile(string path) => _options.FileSystem.OpenFileForReading(path);

    private partial void LogError(Exception exception, string message) => _options.DiagnosticLogger?.LogError(exception, message);

    [UnmanagedCallersOnly(EntryPoint = "sentry_transport_new_default")]
    private static IntPtr CreateDefaultTransport()
    {
        var options = DefaultOptions;
        try
        {
            return options is null ? IntPtr.Zero : new NativeHttpClient(options).CreateTransport();
        }
        catch (Exception e)
        {
            // never allow an exception back to native code - it would crash the app
            options?.DiagnosticLogger?.LogError(e, "Failed to create the sentry-native transport.");
            return IntPtr.Zero;
        }
    }

    [DllImport("sentry-native")]
    private static extern unsafe IntPtr sentry_http_transport_new(
        delegate* unmanaged<IntPtr, IntPtr> factory,
        IntPtr factoryData,
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, int> sendFunc,
        delegate* unmanaged<IntPtr, void> clientFreeFunc);

    [DllImport("sentry-native")]
    private static extern unsafe void sentry_http_transport_set_client_shutdown_func(IntPtr transport, delegate* unmanaged<IntPtr, void> shutdownFunc);

    [DllImport("sentry-native")]
    private static extern IntPtr sentry_http_request_get_method(IntPtr request);

    [DllImport("sentry-native")]
    private static extern IntPtr sentry_http_request_get_url(IntPtr request);

    [DllImport("sentry-native")]
    private static extern UIntPtr sentry_http_request_get_header_count(IntPtr request);

    [DllImport("sentry-native")]
    private static extern int sentry_http_request_get_header(IntPtr request, UIntPtr index, out IntPtr key, out IntPtr value);

    [DllImport("sentry-native")]
    private static extern IntPtr sentry_http_request_get_body(IntPtr request, out UIntPtr length);

    [DllImport("sentry-native")]
    private static extern IntPtr sentry_http_request_get_body_file_path(IntPtr request, out UIntPtr length);

    [DllImport("sentry-native")]
    private static extern UIntPtr sentry_http_request_get_body_file_offset(IntPtr request);

    [DllImport("sentry-native")]
    private static extern void sentry_http_response_set_status_code(IntPtr response, int statusCode);

    [DllImport("sentry-native")]
    private static extern void sentry_http_response_set_header(IntPtr response, string key, string value);
}
