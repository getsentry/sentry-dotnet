#!/usr/bin/env dotnet
#:package Cocona@2.2.0

#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;
using Cocona;

// Simple dev helper CLI implemented as a .NET file-based app using Cocona.
// Usage examples (run from repo root):
//   ./dev.cs --help
//   ./dev.cs cleanslate
//   ./dev.cs cleanslate Sentry-CI-Build-macOS.slnf
//   ./dev.cs subup
//   ./dev.cs wrest
//   ./dev.cs nrest
//   ./dev.cs aiup
//   ./dev.cs cleanslate --dry-run
//
// This is intended to be run from the repo root so that git/dotnet
// commands operate on this repository.

var app = CoconaApp.Create();

app.AddCommands<DevCommands>();

app.Run();

// ----------------- Options & Commands -----------------

public class GlobalOptions : ICommandParameterSet
{
    [Option("dry-run", new[] { 'n' }, Description = "Print commands instead of executing them.")]
    public bool DryRun { get; set; }
}

public class DevCommands
{
    [Command("cleanslate", Description = "Clean repo, update submodules, and restore the solution.")]
    public async Task<int> CleanSlateAsync(
        [Argument("solution", Description = "Solution file to restore. Defaults to platform-specific CI solution if omitted.")] string? solution = null,
        GlobalOptions options = default!)
    {
        solution ??= DevConfig.DefaultSolution;

        Console.WriteLine($"[dev] cleanslate for solution: {solution}");

        var steps = new (string Description, string FileName, string Arguments)[]
        {
            ("git clean", "git", "clean -dfx"),
            ("git submodule update", "git", "submodule update --init --recursive"),
            ("dotnet restore", "dotnet", $"restore \"{solution}\""),
            ("npx @sentry/dotagents install", "npx", "@sentry/dotagents install")
        };

        foreach (var (description, fileName, arguments) in steps)
        {
            int code = await RunStepAsync(description, fileName, arguments, options.DryRun);
            if (code != 0)
            {
                Console.Error.WriteLine($"[dev] Step '{description}' failed with exit code {code}.");
                return code;
            }
        }

        return 0;
    }

    [Command("subup", Description = "Update git submodules recursively.")]
    public Task<int> SubmoduleUpdateAsync(GlobalOptions options = default!)
    {
        Console.WriteLine("[dev] Updating git submodules (recursive)");
        return RunStepAsync("git submodule update", "git", "submodule update --init --recursive", options.DryRun);
    }

    [Command("wrest", Description = "Run 'dotnet workload restore' (with sudo on Unix if available).")]
    public async Task<int> WorkloadRestoreAsync(GlobalOptions options = default!)
    {
        Console.WriteLine("[dev] Restoring dotnet workloads");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // No sudo on Windows
            return await RunStepAsync("dotnet workload restore", "dotnet", "workload restore", options.DryRun);
        }

        bool sudoAvailable = await IsCommandAvailableAsync("sudo");

        if (sudoAvailable)
        {
            return await RunStepAsync("sudo dotnet workload restore", "sudo", "dotnet workload restore", options.DryRun);
        }

        Console.WriteLine("[dev] 'sudo' not found; running 'dotnet workload restore' without sudo.");
        return await RunStepAsync("dotnet workload restore", "dotnet", "workload restore", options.DryRun);
    }

    [Command("aiup", Description = "Install/update AI agent files via @sentry/dotagents.")]
    public Task<int> AiUpdateAsync(GlobalOptions options = default!)
    {
        Console.WriteLine("[dev] Installing/updating AI agent files");
        return RunStepAsync("npx @sentry/dotagents install", "npx", "@sentry/dotagents install", options.DryRun);
    }

    [Command("setup-hooks", Description = "Configure git to use the repo's pre-commit hooks from .githooks/.")]
    public Task<int> SetupHooksAsync(GlobalOptions options = default!)
    {
        Console.WriteLine("[dev] Configuring git hooks path to .githooks/");
        return RunStepAsync("git config core.hooksPath", "git", "config core.hooksPath .githooks", options.DryRun);
    }

    [Command("nrest", Description = "Restore the default CI solution.")]
    public Task<int> SolutionRestoreAsync(
        [Argument("solution", Description = "Solution file to restore. Defaults to platform-specific CI solution if omitted.")] string? solution = null,
        GlobalOptions options = default!)
    {
        solution ??= DevConfig.DefaultSolution;
        Console.WriteLine($"[dev] Restoring solution: {solution}");
        return RunStepAsync("dotnet restore", "dotnet", $"restore \"{solution}\"", options.DryRun);
    }

    private static async Task<int> RunStepAsync(string description, string fileName, string arguments, bool dryRun)
    {
        Console.WriteLine($"==> {description}: {fileName} {arguments}");
        return await RunProcessAsync(fileName, arguments, dryRun);
    }

    private static async Task<int> RunProcessAsync(string fileName, string arguments, bool dryRun)
    {
        if (dryRun)
        {
            Console.WriteLine($"[DRY RUN] {fileName} {arguments}");
            return 0;
        }

        // On Windows, .cmd/.bat files (e.g. npx.cmd) can't be launched via CreateProcess directly;
        // route through cmd.exe so the shell resolves them correctly.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            arguments = $"/c {fileName} {arguments}";
            fileName = "cmd.exe";
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

    private static async Task<bool> IsCommandAvailableAsync(string command)
    {
        // Use 'where.exe' on Windows, 'which' on Unix
        var finder = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where.exe" : "which";

        var startInfo = new ProcessStartInfo
        {
            FileName = finder,
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

        // Consume streams to avoid pipe buffer deadlock, then wait for exit
        await Task.WhenAll(
            process.StandardOutput.ReadToEndAsync(),
            process.StandardError.ReadToEndAsync(),
            process.WaitForExitAsync());

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
