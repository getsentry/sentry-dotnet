using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Extensibility;

namespace Sentry.AspNetCore.Blazor;

internal class WasmStackTraceFactory : ISentryStackTraceFactory
{
    private readonly ISentryStackTraceFactory _stackTraceFactory;
    private readonly SentryBlazorOptions _options;


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
            if (exception.Message is { } message)
            {
                foreach (var line in message.Split('\n'))
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

            var sentryStackTrace = new SentryStackTrace
            {
                // https://develop.sentry.dev/sdk/event-payloads/stacktrace/
                Frames = frames.ToArray().Reverse().ToList()
            };
            return sentryStackTrace;
        }
        return _stackTraceFactory.Create(exception);
    }

    // https://github.com/getsentry/sentry-javascript/blob/2d80b4b2cfabb69f0cfd4a96ea637a8cabbd37cb/packages/wasm/src/index.ts#L13
    private readonly Regex _wasmFrame = new(@"^\s+at (.*?):wasm-function\[\d+\]:(0x[a-fA-F0-9]+)$", RegexOptions.Compiled);

    internal bool TryParse(string line, [MaybeNullWhen(false)] out SentryStackFrame frame)
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
            var images = _options.GetDebugImages?.Invoke();
            if (images is null)
            {
                _options.DiagnosticLogger?.LogError("No debug images found.");
                return false;
            }

            var image = images.FirstOrDefault(i => i.Type == "wasm" && i.CodeFile == match.Groups[1].Value);
            if (image is null)
            {
                _options.DiagnosticLogger?.LogError("Didn't find specific image.");
                return false;
            }
            frame = new SentryStackFrame
            {
                Platform = "native", // https://github.com/getsentry/sentry-javascript/blob/2d80b4b2cfabb69f0cfd4a96ea637a8cabbd37cb/packages/wasm/src/index.ts#L20
                InstructionAddress = match.Groups[2].Value,
                AddressMode = $"rel:{Array.IndexOf(images, image)}",
                FileName = match.Groups[1].Value,
            };
            return true;
        }

        return match.Success;
    }
}
