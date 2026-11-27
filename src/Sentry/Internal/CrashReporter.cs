#if PLATFORM_NEUTRAL
using Sentry.Extensibility;
using Sentry.PlatformAbstractions;
using Sentry.Protocol.Envelopes;

namespace Sentry.Internal;

internal class CrashReporter
{
    private readonly SentryOptions _options;
    private readonly Action<ProcessStartInfo> _startProcess;

    public CrashReporter(SentryOptions options, Action<ProcessStartInfo>? startProcess = null)
    {
        _options = options;
        _startProcess = startProcess ?? (startInfo => Process.Start(startInfo)?.Dispose());
    }

    public bool TryLaunch(Envelope envelope)
    {
        if (_options.CrashReporterPath is not { } path || SentryRuntime.Current.IsBrowserWasm())
        {
            return false;
        }

        var isBundle = IsBundle(path);
        if (!isBundle && !_options.FileSystem.FileExists(path))
        {
            _options.LogError("Crash reporter not found: {0}", path);
            return false;
        }

        try
        {
            if (WriteEnvelope(envelope) is not { } file)
            {
                _options.LogError("Failed to write the envelope for the crash reporter.");
                return false;
            }

            // Stryker disable once all : covered by the macOS tests
            _startProcess(isBundle
                ? CreateStartInfo("/usr/bin/open", "-a", path, "--args", file)
                : CreateStartInfo(path, file));
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to launch crash reporter: {0}", path);
            return false;
        }

        _options.LogInfo("Launched crash reporter: {0}", path);
        return true;
    }

    private string? WriteEnvelope(Envelope envelope)
    {
        var directory = Path.Combine(
            _options.TryGetDsnSpecificCacheDirectoryPath() ?? Path.Combine(Path.GetTempPath(), "Sentry"),
            "external");
        var file = Path.Combine(directory, $"{envelope.TryGetEventId(_options.DiagnosticLogger)}.envelope");
        if (!_options.FileSystem.CreateDirectory(directory) ||
            !_options.FileSystem.CreateFileForWriting(file, out var stream))
        {
            return null;
        }

        var header = envelope.Header.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        header["dsn"] = _options.Dsn;

        using (stream)
        {
            new Envelope(header, envelope.Items).Serialize(stream, _options.DiagnosticLogger);
        }
        return file;
    }

    // Stryker disable once all : covered by the macOS tests
    private bool IsBundle(string path) =>
        RuntimeInformation.IsOSPlatform(OSPlatform.OSX) &&
        path.EndsWith(".app", StringComparison.Ordinal) &&
        _options.FileSystem.DirectoryExists(path);

    private static ProcessStartInfo CreateStartInfo(string fileName, params string[] arguments)
    {
        // Stryker disable once all : covered by the .NET Framework tests
        var startInfo = new ProcessStartInfo(fileName) { UseShellExecute = false };
#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
#else
        startInfo.Arguments = string.Join(" ", arguments.Select(argument => $"\"{argument}\""));
#endif
        return startInfo;
    }
}
#endif
