using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore.Blazor;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder UseSentry(this WebAssemblyHostBuilder builder, Action<SentryBlazorOptions> configureOptions)
    {
        builder.Services.TryAddSingleton<IScopeObserver, JavaScriptScopeObserver>();
        builder.Logging.AddSentry<SentryBlazorOptions>(blazorOptions =>
        {
            configureOptions(blazorOptions);

            blazorOptions.EnableScopeSync = true;
            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            blazorOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            blazorOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
            blazorOptions.UseStackTraceFactory(
                new WasmStackTraceFactory(
                    new SentryStackTraceFactory(blazorOptions),
                    blazorOptions));
        });
        return builder;
    }
}

public class SentryBlazorOptions : SentryLoggingOptions
{
}

internal class WasmStackTraceFactory : ISentryStackTraceFactory
{
    private readonly ISentryStackTraceFactory _stackTraceFactory;
    private readonly SentryBlazorOptions _options;

    // https://github.com/getsentry/sentry-javascript/blob/2d80b4b2cfabb69f0cfd4a96ea637a8cabbd37cb/packages/wasm/src/index.ts#L13
    private readonly Regex _wasmFrame = new(@"^(.*?):wasm-function\[\d+\]:(0x[a-fA-F0-9]+)$", RegexOptions.Compiled);

    public WasmStackTraceFactory(ISentryStackTraceFactory stackTraceFactory, SentryBlazorOptions options)
    {
        _stackTraceFactory = stackTraceFactory;
        _options = options;
    }

    public SentryStackTrace? Create(Exception? exception = null)
    {
        List<SentryStackFrame>? frames = null;
        if (exception is not null && exception.Message.Contains("wasm"))
        {
            if (exception.StackTrace is { } stacktrace)
            {
                foreach (var line in stacktrace.Split('\n'))
                {
                    if (TryParse(line, out var frame))
                    {
                        frames ??= new List<SentryStackFrame>();
                        frames.Add(frame);
                    }
                }
            }

            if (frames is null)
            {
                _options.DiagnosticLogger?.LogWarning("Couldn't parse Wasm stack frames, calling fallback");
                return _stackTraceFactory.Create(exception);
            }

            return new SentryStackTrace
            {
                // https://develop.sentry.dev/sdk/event-payloads/stacktrace/
                // Frames = frames.Select(f => new SentryStackFrame
                // {
                //     Module = f.TypeFullName,
                //     InstructionOffset = f.Offset != 0 ? f.Offset : (long?)null,
                //     Function = f.MethodSignature,
                //     LineNumber = GetLineNumber(f.Line),
                // }).Reverse().ToArray()
            };
        }
        return _stackTraceFactory.Create(exception);
    }

    private bool TryParse(string line, [MaybeNullWhen(false)] out SentryStackFrame frame)
    {
        // https://github.com/getsentry/sentry-javascript/blob/2d80b4b2cfabb69f0cfd4a96ea637a8cabbd37cb/packages/wasm/src/index.ts#L15C1-L21C25
        // const index = getImage(match[1]);
        // if (index >= 0) {
        //     frame.instruction_addr = match[2];
        //     frame.addr_mode = `rel:${index}`;
        //     frame.filename = match[1];
        //     frame.platform = 'native';
        //     haveWasm = true;
        var match = _wasmFrame.Match(line);
        frame = null;
        if (match.Success)
        {
            frame = new SentryStackFrame
            {
                Platform = "native"
            };
            return true;
        }

        return match.Success;
    }
}
