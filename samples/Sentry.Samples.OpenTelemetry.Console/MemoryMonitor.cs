using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Sentry.Samples.OpenTelemetry.Console;

internal class MemoryMonitor
{
    private readonly long _thresholdBytes;
    private readonly long _totalMemory;
    private bool _thresholdDumpCaptured;

    public MemoryMonitor(short thresholdPercentage)
    {
        if (thresholdPercentage is < 0 or > 100)
        {
            throw new ArgumentException("Must be a value between 0 and 100", nameof(thresholdPercentage));
        }

        _totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var portion = (double)thresholdPercentage / 100;
        _thresholdBytes = (long)Math.Ceiling(portion * _totalMemory);
        System.Console.WriteLine("Memory dump will be triggered if memory usage exceeds {0:N0} bytes ({1}%)", _thresholdBytes, thresholdPercentage);

        GarbageCollectionMonitor.Start(CheckMemoryUsage);
    }

    private void CheckMemoryUsage()
    {
        // Get the memory used by the application
        var usedMemory = Environment.WorkingSet;

        // Calculate the percentage of memory used
        // var usedMemoryPercentage = GC.GetGCMemoryInfo().MemoryLoadBytes;
        var usedMemoryPercentage = ((double)usedMemory / _totalMemory) * 100;
        System.Console.WriteLine("Total Memory: {0:N0} bytes", _totalMemory);
        System.Console.WriteLine("Threshold: {0:N0} bytes", _thresholdBytes);
        System.Console.WriteLine("Memory used: {0:N0} bytes ({1:N2}%)", usedMemory, usedMemoryPercentage);

        // Trigger the event if the threshold is exceeded
        if (usedMemory > _thresholdBytes && !_thresholdDumpCaptured)
        {
            _thresholdDumpCaptured = true;
            CaptureMemoryDump();
        }
    }

    internal void CaptureMemoryDump()
    {
        var processId = Environment.ProcessId;
        System.Console.WriteLine($"Creating a memory dump for process: {processId}");

        var command = $"dotnet-gcdump collect -p {processId}";
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
            var line = process.StandardOutput.ReadLine();
            System.Console.WriteLine(line);
        }

        System.Console.WriteLine("Memory dump created");
    }
}
