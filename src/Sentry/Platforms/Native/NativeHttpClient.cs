using Sentry.Internal;

// Stryker disable all : only runs in Native AOT apps
namespace Sentry.Native;

/// <summary>
/// Executes the HTTP requests of the sentry-native transport with an <see cref="HttpClient"/>.
/// </summary>
/// <remarks>
/// Shared by the app and the sentry-crash transport library, which provide the host-specific parts.
/// </remarks>
internal sealed partial class NativeHttpClient
{
    private readonly CancellationTokenSource _shutdown = new();
    private HttpClient? _client;

    private partial HttpClient CreateHttpClient();

    private partial Stream OpenFile(string path);

    private partial void LogError(Exception exception, string message);

    private unsafe IntPtr CreateTransport()
    {
        var handle = GCHandle.Alloc(this);
        // sentry-native frees the handle through FreeClient, also when this fails
        var transport = sentry_http_transport_new(&CreateClient, GCHandle.ToIntPtr(handle), &SendRequest, &FreeClient);
        if (transport != IntPtr.Zero)
        {
            sentry_http_transport_set_client_shutdown_func(transport, &ShutdownClient);
        }
        return transport;
    }

    private void Send(IntPtr nativeRequest, IntPtr nativeResponse)
    {
        using var file = OpenBodyFile(nativeRequest);
        using var request = CreateRequest(nativeRequest, file);
        _client ??= CreateHttpClient();
        // runs on the sentry-native transport thread, which has no synchronization context
        using var response = _client.SendAsync(request, _shutdown.Token).GetAwaiter().GetResult();

        sentry_http_response_set_status_code(nativeResponse, (int)response.StatusCode);
        foreach (var header in response.Headers.Concat(response.Content.Headers))
        {
            sentry_http_response_set_header(nativeResponse, header.Key, string.Join(", ", header.Value));
        }
    }

    private Stream? OpenBodyFile(IntPtr nativeRequest)
    {
        var path = sentry_http_request_get_body_file_path(nativeRequest, out _);
        return path == IntPtr.Zero ? null : OpenFile(Marshal.PtrToStringUTF8(path)!);
    }

    private static HttpRequestMessage CreateRequest(IntPtr nativeRequest, Stream? file)
    {
        var method = Marshal.PtrToStringUTF8(sentry_http_request_get_method(nativeRequest))!;
        var url = Marshal.PtrToStringUTF8(sentry_http_request_get_url(nativeRequest))!;
        var request = new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = CreateContent(nativeRequest, file)
        };

        var count = (int)sentry_http_request_get_header_count(nativeRequest);
        for (var i = 0; i < count; i++)
        {
            if (sentry_http_request_get_header(nativeRequest, (UIntPtr)i, out var keyPtr, out var valuePtr) == 0)
            {
                continue;
            }

            var key = Marshal.PtrToStringUTF8(keyPtr)!;
            var value = Marshal.PtrToStringUTF8(valuePtr);
            // HttpClient computes the length of the content, which a handler may compress
            if (key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!request.Headers.TryAddWithoutValidation(key, value))
            {
                request.Content ??= new ByteArrayContent(Array.Empty<byte>());
                request.Content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        return request;
    }

    private static unsafe HttpContent? CreateContent(IntPtr nativeRequest, Stream? file)
    {
        // only valid until SendRequest returns
        var body = sentry_http_request_get_body(nativeRequest, out var length);
        if (body != IntPtr.Zero)
        {
            return new StreamContent(new UnmanagedMemoryStream((byte*)body, (long)length));
        }

        if (file is not null)
        {
            sentry_http_request_get_body_file_path(nativeRequest, out length);
            var offset = sentry_http_request_get_body_file_offset(nativeRequest);
            return new StreamContent(new PartialStream(file, (long)offset, (long)length));
        }

        return null;
    }

    private static NativeHttpClient FromHandle(IntPtr client) => (NativeHttpClient)GCHandle.FromIntPtr(client).Target!;

    [UnmanagedCallersOnly]
    private static IntPtr CreateClient(IntPtr handle) => handle;

    [UnmanagedCallersOnly]
    private static int SendRequest(IntPtr client, IntPtr request, IntPtr response)
    {
        var self = FromHandle(client);
        try
        {
            self.Send(request, response);
            return 1;
        }
        catch (Exception e)
        {
            // never allow an exception back to native code - it would crash the app
            self.LogError(e, "Failed to send a sentry-native request.");
            return 0;
        }
    }

    [UnmanagedCallersOnly]
    private static void ShutdownClient(IntPtr client)
    {
        var self = FromHandle(client);
        try
        {
            self._shutdown.Cancel();
        }
        catch (Exception e)
        {
            // never allow an exception back to native code - it would crash the app
            self.LogError(e, "Failed to cancel the sentry-native requests.");
        }
    }

    [UnmanagedCallersOnly]
    private static void FreeClient(IntPtr client)
    {
        var handle = GCHandle.FromIntPtr(client);
        var self = (NativeHttpClient)handle.Target!;
        try
        {
            self._client?.Dispose();
            self._shutdown.Dispose();
        }
        catch (Exception e)
        {
            // never allow an exception back to native code - it would crash the app
            self.LogError(e, "Failed to dispose the HTTP client for sentry-native.");
        }
        finally
        {
            handle.Free();
        }
    }
}
