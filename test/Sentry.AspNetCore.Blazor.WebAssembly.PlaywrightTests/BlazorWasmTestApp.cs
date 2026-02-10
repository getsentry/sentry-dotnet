using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests;

internal sealed class BlazorWasmTestApp : IAsyncDisposable
{
    private Process? _process;

    public string BaseUrl { get; private set; } = null!;

    public async Task StartAsync()
    {
        var port = GetFreePort();
        BaseUrl = $"http://localhost:{port}";

        var projectPath = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests.TestApp"));

        _process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --no-build --project \"{projectPath}\" --urls {BaseUrl}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };
        _process.Start();

        // Discard stdout/stderr to prevent buffer deadlock
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var http = new HttpClient();
        var timeout = TimeSpan.FromSeconds(60);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            try
            {
                var response = await http.GetAsync(BaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }
            await Task.Delay(500);
        }

        throw new TimeoutException($"Blazor WASM test app did not start within {timeout.TotalSeconds}s at {BaseUrl}");
    }

    private static int GetFreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }
        _process?.Dispose();
    }
}
