namespace Sentry.Native;

internal sealed partial class NativeHttpClient
{
    // a shared library can't link against the sentry-native symbols in sentry-crash
    private static Api _api;

    private partial HttpClient CreateHttpClient(IntPtr nativeOptions)
    {
        string? proxy, userAgent;
        unsafe
        {
            proxy = Marshal.PtrToStringUTF8(_api.sentry_options_get_proxy(nativeOptions));
            userAgent = Marshal.PtrToStringUTF8(_api.sentry_options_get_user_agent(nativeOptions));
        }

        var handler = new SocketsHttpHandler();
        // null means the system proxy, and empty means no proxy
        if (proxy is not null)
        {
            handler.UseProxy = proxy.Length > 0;
            if (handler.UseProxy)
            {
                handler.Proxy = new WebProxy(proxy);
            }
        }

        var client = new HttpClient(handler);
        if (userAgent is not null)
        {
            client.DefaultRequestHeaders.UserAgent.TryParseAdd(userAgent);
        }
        return client;
    }

    private partial Stream OpenFile(string path) => File.OpenRead(path);

    // sentry-crash has no logger for the transport
    private partial void LogError(Exception exception, string message)
    {
    }

    [UnmanagedCallersOnly(EntryPoint = "sentry_dotnet_transport_new")]
    private static unsafe IntPtr CreateDaemonTransport(Api* api)
    {
        try
        {
            _api = *api;
            return new NativeHttpClient().CreateTransport();
        }
        catch
        {
            // never allow an exception back to native code - it would crash the daemon
            return IntPtr.Zero;
        }
    }

    private static unsafe IntPtr sentry_http_transport_new(
        delegate* unmanaged<IntPtr, IntPtr> factory,
        IntPtr factoryData,
        delegate* unmanaged<IntPtr, IntPtr, IntPtr, int> sendFunc,
        delegate* unmanaged<IntPtr, void> clientFreeFunc) =>
        _api.sentry_http_transport_new((IntPtr)factory, factoryData, (IntPtr)sendFunc, (IntPtr)clientFreeFunc);

    private static unsafe void sentry_http_transport_set_client_start_func(IntPtr transport, delegate* unmanaged<IntPtr, IntPtr, int> startFunc) =>
        _api.sentry_http_transport_set_client_start_func(transport, (IntPtr)startFunc);

    private static unsafe void sentry_http_transport_set_client_shutdown_func(IntPtr transport, delegate* unmanaged<IntPtr, void> shutdownFunc) =>
        _api.sentry_http_transport_set_client_shutdown_func(transport, (IntPtr)shutdownFunc);

    private static unsafe IntPtr sentry_http_request_get_method(IntPtr request) =>
        _api.sentry_http_request_get_method(request);

    private static unsafe IntPtr sentry_http_request_get_url(IntPtr request) =>
        _api.sentry_http_request_get_url(request);

    private static unsafe UIntPtr sentry_http_request_get_header_count(IntPtr request) =>
        _api.sentry_http_request_get_header_count(request);

    private static unsafe int sentry_http_request_get_header(IntPtr request, UIntPtr index, out IntPtr key, out IntPtr value)
    {
        IntPtr k = default, v = default;
        var result = _api.sentry_http_request_get_header(request, index, &k, &v);
        key = k;
        value = v;
        return result;
    }

    private static unsafe IntPtr sentry_http_request_get_body(IntPtr request, out UIntPtr length)
    {
        UIntPtr len = default;
        var result = _api.sentry_http_request_get_body(request, &len);
        length = len;
        return result;
    }

    private static unsafe IntPtr sentry_http_request_get_body_file_path(IntPtr request, out UIntPtr length)
    {
        UIntPtr len = default;
        var result = _api.sentry_http_request_get_body_file_path(request, &len);
        length = len;
        return result;
    }

    private static unsafe UIntPtr sentry_http_request_get_body_file_offset(IntPtr request) =>
        _api.sentry_http_request_get_body_file_offset(request);

    private static unsafe void sentry_http_response_set_status_code(IntPtr response, int statusCode) =>
        _api.sentry_http_response_set_status_code(response, statusCode);

    private static unsafe void sentry_http_response_set_header(IntPtr response, string key, string value)
    {
        var k = Marshal.StringToCoTaskMemUTF8(key);
        var v = Marshal.StringToCoTaskMemUTF8(value);
        try
        {
            _api.sentry_http_response_set_header(response, k, v);
        }
        finally
        {
            Marshal.FreeCoTaskMem(k);
            Marshal.FreeCoTaskMem(v);
        }
    }

    /// <summary>
    /// The sentry-native functions that sentry-crash passes to <c>sentry_dotnet_transport_new()</c>, in this order.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct Api
    {
        public delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr> sentry_http_transport_new;
        public delegate* unmanaged<IntPtr, IntPtr, void> sentry_http_transport_set_client_start_func;
        public delegate* unmanaged<IntPtr, IntPtr, void> sentry_http_transport_set_client_shutdown_func;
        public delegate* unmanaged<IntPtr, IntPtr> sentry_http_request_get_method;
        public delegate* unmanaged<IntPtr, IntPtr> sentry_http_request_get_url;
        public delegate* unmanaged<IntPtr, UIntPtr> sentry_http_request_get_header_count;
        public delegate* unmanaged<IntPtr, UIntPtr, IntPtr*, IntPtr*, int> sentry_http_request_get_header;
        public delegate* unmanaged<IntPtr, UIntPtr*, IntPtr> sentry_http_request_get_body;
        public delegate* unmanaged<IntPtr, UIntPtr*, IntPtr> sentry_http_request_get_body_file_path;
        public delegate* unmanaged<IntPtr, UIntPtr> sentry_http_request_get_body_file_offset;
        public delegate* unmanaged<IntPtr, int, void> sentry_http_response_set_status_code;
        public delegate* unmanaged<IntPtr, IntPtr, IntPtr, void> sentry_http_response_set_header;
        public delegate* unmanaged<IntPtr, IntPtr> sentry_options_get_proxy;
        public delegate* unmanaged<IntPtr, IntPtr> sentry_options_get_user_agent;
    }
}
