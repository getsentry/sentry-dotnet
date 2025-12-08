#!/usr/bin/env dotnet
#:package Cocona@2.2.0
#:package Microsoft.Extensions.DependencyInjection@8.0.0

#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Cocona;

// Simple dev helper CLI implemented as a .NET file-based app using Cocona.
// Usage examples (run from repo root):
//   ./dev.cs --help
//   ./dev.cs cleanslate
//   ./dev.cs cleanslate Sentry-CI-Build-macOS.slnf
//   ./dev.cs subup
//   ./dev.cs wrest
//   ./dev.cs nrest
//   ./dev.cs --dry-run cleanslate
//
// This is intended to be run from the repo root so that git/dotnet
// commands operate on this repository.

var builder = CoconaApp.CreateBuilder();
builder.Services.AddSingleton<IDevProcessRunner>(sp =>
{
    var options = sp.GetRequiredService<GlobalOptions>();
    return new DevProcessRunner(options.DryRun);
});
builder.Services.AddSingleton<GlobalOptions>();
var app = builder.Build();

app.AddCommands<DevCommands>();

await app.RunAsync();

// ----------------- Options & Commands -----------------

public class GlobalOptions
{
    [Option("dry-run", new[] { 'n' }, Description = "Print commands instead of executing them.")]
    public bool DryRun { get; set; }
}

public class DevCommands
{
    private readonly IDevProcessRunner _runner;

    public DevCommands(IDevProcessRunner runner)
    {
        _runner = runner;
    }

    [Command("cleanslate", Description = "Clean repo, update submodules, and restore the solution.")]
    public async Task<int> CleanSlateAsync(
        [Argument("solution", Description = "Solution file to restore. Defaults to Sentry-CI-Build-macOS.slnf if omitted.")] string? solution = null)
    {
        solution ??= DevConfig.DefaultSolution;

        Console.WriteLine($"[dev] cleanslate for solution: {solution}");

        var steps = new (string Description, string FileName, string Arguments)[]
        {
            ("git clean", "git", "clean -dfx"),
            ("git submodule update", "git", "submodule update --recursive"),
            ("dotnet restore", "dotnet", $"restore \"{solution}\"")
        };

        foreach (var (description, fileName, arguments) in steps)
        {
            int code = await _runner.RunStepAsync(description, fileName, arguments);
            if (code != 0)
            {
                Console.Error.WriteLine($"[dev] Step '{description}' failed with exit code {code}.");
                return code;
            }
        }

        return 0;
    }

    [Command("subup", Description = "Update git submodules recursively.")]
    public Task<int> SubmoduleUpdateAsync()
    {
        Console.WriteLine("[dev] Updating git submodules (recursive)");
        return _runner.RunStepAsync("git submodule update", "git", "submodule update --recursive");
    }

    [Command("wrest", Description = "Run 'dotnet workload restore' (with sudo on Unix if available).")]
    public async Task<int> WorkloadRestoreAsync()
    {
        Console.WriteLine("[dev] Restoring dotnet workloads");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // No sudo on Windows
            return await _runner.RunStepAsync("dotnet workload restore", "dotnet", "workload restore");
        }

        bool sudoAvailable = await _runner.IsCommandAvailableAsync("sudo");

        if (sudoAvailable)
        {
            return await _runner.RunStepAsync("sudo dotnet workload restore", "sudo", "dotnet workload restore");
        }

        Console.WriteLine("[dev] 'sudo' not found; running 'dotnet workload restore' without sudo.");
        return await _runner.RunStepAsync("dotnet workload restore", "dotnet", "workload restore");
    }

    [Command("nrest", Description = "Restore the default CI solution.")]
    public Task<int> SolutionRestoreAsync(
        [Argument("solution", Description = "Solution file to restore. Defaults to Sentry-CI-Build-macOS.slnf if omitted.")] string? solution = null)
    {
        solution ??= DevConfig.DefaultSolution;
        Console.WriteLine($"[dev] Restoring solution: {solution}");
        return _runner.RunStepAsync("dotnet restore", "dotnet", $"restore \"{solution}\"");
    }
}

// ----------------- Process helpers -----------------

public interface IDevProcessRunner
{
    Task<int> RunStepAsync(string description, string fileName, string arguments);
    Task<bool> IsCommandAvailableAsync(string command);
}

public class DevProcessRunner : IDevProcessRunner
{
    private readonly bool _dryRun;

    public DevProcessRunner(bool dryRun)
    {
        _dryRun = dryRun;
    }

    public Task<int> RunStepAsync(string description, string fileName, string arguments)
    {
        Console.WriteLine($"==> {description}: {fileName} {arguments}");
        return RunProcessAsync(fileName, arguments);
    }

    private async Task<int> RunProcessAsync(string fileName, string arguments)
    {
        if (_dryRun)
        {
            Console.WriteLine($"[DRY RUN] {fileName} {arguments}");
            return 0;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                Console.Error.WriteLine($"[dev] Failed to start process: {fileName}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[dev] Exception starting process '{fileName}': {ex.Message}");
            return 1;
        }

        await process.WaitForExitAsync();
        return process.ExitCode;
    }

    public async Task<bool> IsCommandAvailableAsync(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return await Task.FromResult(true);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = "which",
            Arguments = command,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                return false;
            }
        }
        catch
        {
            return false;
        }

        await process.WaitForExitAsync();
        return process.ExitCode == 0;
    }
}


// Central configuration/constants for the dev script.
public static class DevConfig
{
    public static string DefaultSolution
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "Sentry-CI-Build-Windows.slnf";
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "Sentry-CI-Build-Linux.slnf";
            }

            // Fallback: macOS solution
            return "Sentry-CI-Build-macOS.slnf";
        }
    }
}
