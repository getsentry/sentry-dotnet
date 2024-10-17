/*
 * dotnet-gcdump needs .NET 6 or later:
 * https://www.nuget.org/packages/dotnet-gcdump#supportedframeworks-body-tab
 *
 * Also `GC.GetGCMemoryInfo()` is not available in NetFX or NetStandard
 */
#if NET6_0_OR_GREATER && !(IOS || ANDROID)

using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Internal;

internal class MemoryMonitor : IDisposable
{
    private readonly long _totalMemory;

    private readonly SentryOptions _options;
    private readonly HeapDumpTrigger _dumpTrigger;

    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private readonly Action _onCaptureDump; // Just for testing purposes
    private readonly Action<string> _onDumpCollected;

    public MemoryMonitor(SentryOptions options, Action<string> onDumpCollected, Action? onCaptureDump = null)
    {
        _options = options;
        _dumpTrigger = options.HeapDumpTrigger
                       ?? throw new ArgumentException("No heap dump trigger configured on the options", nameof(options));
        _onDumpCollected = onDumpCollected;
        _onCaptureDump = onCaptureDump ?? CaptureMemoryDump;

        _totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

        GarbageCollectionMonitor.Start(CheckMemoryUsage, _cancellationTokenSource.Token);
    }

    internal void CheckMemoryUsage()
    {
        var eventTime = DateTimeOffset.UtcNow;
        if (!_options.HeapDumpDebouncer.CanProcess(eventTime))
        {
            return;
        }

        var usedMemory = Environment.WorkingSet;
        if (!_dumpTrigger(usedMemory, _totalMemory))
        {
            return;
        }

        _options.HeapDumpDebouncer.RecordOccurence(eventTime);

        var usedMemoryPercentage = ((double)usedMemory / _totalMemory) * 100;
        _options.LogDebug("Total Memory: {0:N0} bytes", _totalMemory);
        _options.LogDebug("Memory used: {0:N0} bytes ({1:N2}%)", usedMemory, usedMemoryPercentage);
        _options.LogDebug("Automatic heap dump triggered");
        _onCaptureDump();
    }

    internal void CaptureMemoryDump()
    {
        if (_options.DisableFileWrite)
        {
            _options.LogDebug("File write has been disabled via the options. Unable to create memory dump.");
            return;
        }

        var dumpFile = TryGetDumpLocation();
        if (dumpFile is null)
        {
            return;
        }

        var processId = Environment.ProcessId;
        _options.LogInfo("Creating a memory dump for Process ID: {0}", processId);

        // Check which patth to use for dotnet-gcdump. If it's been bundled with the application, it will be available
        // in the `dotnet-gcdump` folder of the application directory. Otherwise we assume it has been installed globally.
        var bundledToolPath = Path.Combine(AppContext.BaseDirectory, "dotnet-gcdump", "dotnet-gcdump.dll");
        if (File.Exists(bundledToolPath))
        {
            _options.LogDebug($"Using bundled version of dotnet-gcdump from: {bundledToolPath}");
        }
        else
        {
            _options.LogDebug("Using global version of dotnet-gcdump");
        }

        var arguments = $"collect -p {processId} -o '{dumpFile}'";
        var command = File.Exists(bundledToolPath)
            ? $"dotnet {bundledToolPath} {arguments}"
            : $"dotnet-gcdump {arguments}";
        var startInfo = new ProcessStartInfo
        {
            FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "cmd.exe" : "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        var process = new Process { StartInfo = startInfo };
        process.Start();
        while (!process.StandardOutput.EndOfStream)
        {
            if (process.StandardOutput.ReadLine() is { } line)
            {
                _options.LogDebug(line);
            }
        }

        if (!_options.FileSystem.FileExists(dumpFile))
        {
            // if this happens, hopefully there would be more information in the standard output from the process above
            _options.LogError("Unexpected error creating memory dump. Check debug logs for more information.");
        }

        _onDumpCollected(dumpFile);
    }

    internal string? TryGetDumpLocation()
    {
        try
        {
            var rootPath = _options.CacheDirectoryPath ??
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var directoryPath = Path.Combine(rootPath, "Sentry", _options.Dsn!.GetHashString());
            var fileSystem = _options.FileSystem;

            if (!fileSystem.CreateDirectory(directoryPath))
            {
                _options.LogWarning("Failed to create a directory for memory dump ({0}).", directoryPath);
                return null;
            }
            _options.LogDebug("Created directory for heap dump ({0}).", directoryPath);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var processId = Environment.ProcessId;
            var filePath = Path.Combine(directoryPath, $"{timestamp}_{processId}.gcdump");
            if (fileSystem.FileExists(filePath))
            {
                _options.LogWarning("Duplicate dump file detected.");
                return null;
            }

            return filePath;
        }
        // If there's no write permission or the platform doesn't support this, we handle simply log and bug out
        catch (Exception ex)
        {
            _options.LogError(ex, "Failed to resolve appropriate memory dump location.");
            return null;
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
    }
}

#endif
