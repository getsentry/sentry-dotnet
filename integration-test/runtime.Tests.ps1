# This file contains test cases for https://pester.dev/
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. $PSScriptRoot/common.ps1

Describe 'Console app NativeAOT (<framework>)' -ForEach @(
    @{ framework = "net8.0" }
) {
    BeforeAll {
        $path = './console-app'
        DotnetNew 'console' $path $framework
        @"
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

// Initialize the Sentry SDK.  (It is not necessary to dispose it.)
SentrySdk.Init(options =>
{
    options.Dsn = "http://key@127.0.0.1:9999/123";
    options.Debug = true;
    options.Transport = new FakeTransport();
});

throw new ApplicationException("Something happened!");

internal class FakeTransport : ITransport
{
    public virtual Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
    {
        envelope.Serialize(Console.OpenStandardOutput(), null);
        return Task.CompletedTask;
    }
}
"@ | Out-File $path/Program.cs

        # Publish once, then run the executable in actual tests.
        dotnet publish console-app -c Release --nologo --framework $framework | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to publish the test app project."
        }

        function getConsoleAppPath()
        {
            if ($IsMacOS)
            {
                $arch = $(uname -m) -eq 'arm64' ? 'arm64' : 'x64'
                return "./console-app/bin/Release/$framework/osx-$arch/publish/console-app"
            }
            elseif ($IsWindows)
            {
                return "./console-app/bin/Release/$framework/win-x64/publish/console-app.exe"
            }
            else
            {
                return "./console-app/bin/Release/$framework/linux-x64/publish/console-app"
            }
        }

        function runConsoleApp([bool]$IsAOT = $true)
        {
            $executable = $IsAOT ? { & (getConsoleAppPath) } : { dotnet run --project $path -c Release --framework $framework }
            Write-Host "::group::Executing $executable"
            try
            {
                $executable.Invoke() | ForEach-Object {
                    Write-Host "  $_"
                    $_
                }
            }
            finally
            {
                Write-Host "::endgroup::"
            }
        }
    }

    It "sends native debug images" {
        runConsoleApp | Should -AnyElementMatch '"debug_meta":{"images":\[{"type":"(pe|elf|macho)","image_addr":"0x[a-f0-9]+","image_size":[0-9]+,"debug_id":"[a-f0-9\-]+"'
    }

    It "sends stack trace native addresses" {
        runConsoleApp | Should -AnyElementMatch '"stacktrace":{"frames":\[{"image_addr":"0x[a-f0-9]+","instruction_addr":"0x[a-f0-9]+"}'
    }

    It "publish directory contains expected files" {
        $path = getConsoleAppPath
        Test-Path $path | Should -BeTrue
        $items = Get-ChildItem -Path (Get-Item $path).DirectoryName
        $exeExtension = $IsWindows ? '.exe' : ''
        $debugExtension = $IsWindows ? '.pdb' : $IsMacOS ? '.dSYM' : '.dbg'
        $items | ForEach-Object { $_.Name } | Sort-Object -Unique | Should -Be (@(
                "console-app$exeExtension", "console-app$debugExtension") | Sort-Object -Unique)
    }

    It "'dotnet publish' produces an app that's recognized as AOT by Sentry" {
        runConsoleApp | Should -AnyElementMatch 'This looks like a NativeAOT application build.'
    }

    It "'dotnet run' produces an app that's recognized as JIT by Sentry" {
        runConsoleApp $false | Should -AnyElementMatch 'This looks like a standard JIT/AOT application build.'
    }
}

# This ensures we don't have a regression for https://github.com/getsentry/sentry-dotnet/issues/2825
Describe 'Console app regression (missing System.Reflection.Metadata)' -Skip:(!$IsWindows) {
    AfterAll {
        dotnet remove ./net4-console/console-app.csproj package Sentry
    }

    It "Ensure System.Reflection.Metadata is not missing" {
        $path = './net4-console'
        Remove-Item -Recurse -Force -Path @("$path/bin", "$path/obj") -ErrorAction SilentlyContinue
        AddPackageReference $path 'Sentry'

        function runConsoleApp()
        {
            $executable = { dotnet run --project $path -c Release }
            Write-Host "::group::Executing $executable"
            try
            {
                $executable.Invoke() | ForEach-Object {
                    Write-Host "  $_"
                    $_
                }
            }
            finally
            {
                Write-Host "::endgroup::"
            }
        }

        $output = runConsoleApp
        $output | Should -Not -AnyElementMatch 'Could not load file or assembly.'
        $output | Should -AnyElementMatch '"exception":{"values":\[{"type":"System.ApplicationException","value":"Something happened!"'
    }
}
