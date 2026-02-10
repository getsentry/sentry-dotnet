using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests;

internal sealed class BlazorWasmTestApp : IAsyncDisposable
{
    private Process? _process;
    private readonly ConcurrentQueue<string> _output = new();

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
                Arguments = $"run --project \"{projectPath}\" --urls {BaseUrl}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            }
        };
        _process.OutputDataReceived += (_, e) => { if (e.Data != null) _output.Enqueue($"[stdout] {e.Data}"); };
        _process.ErrorDataReceived += (_, e) => { if (e.Data != null) _output.Enqueue($"[stderr] {e.Data}"); };
        _process.Start();
        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        using var http = new HttpClient();
        var timeout = TimeSpan.FromSeconds(180);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            if (_process.HasExited)
            {
                var logs = string.Join(Environment.NewLine, _output);
                throw new InvalidOperationException(
                    $"Blazor WASM test app exited with code {_process.ExitCode} before becoming ready. Output:{Environment.NewLine}{logs}");
            }

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

        var timeoutLogs = string.Join(Environment.NewLine, _output);
        throw new TimeoutException(
            $"Blazor WASM test app did not start within {(int)timeout.TotalSeconds}s at {BaseUrl}. Output:{Environment.NewLine}{timeoutLogs}");
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
